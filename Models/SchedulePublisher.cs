using Microsoft.EntityFrameworkCore;

namespace OpenIDApp.Models
{
    public class SchedulePublisher
    {
        private readonly OpenIDContext _db;
        private readonly IConfiguration _cfg;

        public SchedulePublisher(OpenIDContext db, IConfiguration cfg)
        {
            _db = db;
            _cfg = cfg;
        }

        public async Task Publish()
        {
            //  Tham số từ appsettings (có fallback) 
            var baseDateStr =
                  _cfg["Scheduling:BaseDateLocal"]
               ?? _cfg["Scheduling:BaseDate"]
               ?? "2025-11-10";
            var baseDate = DateOnly.Parse(baseDateStr);

            int duration     = int.Parse(_cfg["Scheduling:DurationMinutes"] ?? "90");
            int cap          = int.Parse(_cfg["Scheduling:RoomCapacity"]     ?? "45");
            int slotsPerDay  = TimeSlot.SlotsPerDay; // 4 ca/ngày

            // Dọn dữ liệu cũ 
            _db.StudentExams.RemoveRange(_db.StudentExams);
            _db.Exams.RemoveRange(_db.Exams);
            await _db.SaveChangesAsync();

            //  Sĩ số mỗi môn (từ student_subjects) 
            var enrolled = await _db.Set<StudentSubject>()
                .GroupBy(x => x.SubjectId)
                .Select(g => new { SubjectId = g.Key, Remaining = g.Count() })
                .ToListAsync();
            if (enrolled.Count == 0) return;

            var remain = enrolled.ToDictionary(x => x.SubjectId, x => x.Remaining);

            //  Danh sách phòng 
            var rooms = await _db.Rooms
                .OrderBy(r => r.RoomId)
                .Select(r => r.RoomId)
                .ToListAsync();
            int R = rooms.Count; // số phòng khả dụng

            //  Lưu danh sách examId theo từng môn 
            var examsBySubject = new Dictionary<int, List<int>>();

            //  Lặp theo "ca" (tick) 
            int tick = 0;
            while (remain.Values.Any(v => v > 0))
            {
                // Ưu tiên môn còn đông
                var order = remain.Where(kv => kv.Value > 0)
                                  .OrderByDescending(kv => kv.Value)
                                  .Select(kv => kv.Key)
                                  .ToList();
                if (order.Count == 0) break;

                // Cấp tối đa R phòng trong ca này
                var allocation = new List<int>();
                int i = 0;
                while (allocation.Count < R && i < order.Count)
                {
                    int s = order[i++];
                    int roomsNeed = (int)Math.Ceiling(remain[s] / (double)cap);
                    int free = R - allocation.Count;
                    int take = Math.Min(roomsNeed, free);
                    for (int t = 0; t < take; t++) allocation.Add(s);
                }

                // Thời gian của ca này
                int dayIndex = tick / slotsPerDay;
                int slotId   = tick % slotsPerDay;
                var (start, _) = TimeSlot.GetSlotLocalTime(slotId); // TimeSpan
                var when = baseDate
                    .ToDateTime(TimeOnly.FromTimeSpan(start))  // <-- fix TimeOnly
                    .AddDays(dayIndex);

                // Tạo exams cho các phòng được cấp
                for (int r = 0; r < allocation.Count; r++)
                {
                    int subjectId = allocation[r];
                    int roomId    = rooms[r];

                    _db.Exams.Add(new Exam
                    {
                        SubjectId       = subjectId,
                        RoomId          = roomId,
                        Date            = when,       // local time
                        DurationMinutes = duration
                    });
                }
                await _db.SaveChangesAsync();

                // Lấy các exam vừa tạo
                var createdNow = await _db.Exams
                    .Where(e => e.Date == when)
                    .OrderBy(e => e.RoomId)
                    .Select(e => new { e.ExamId, e.SubjectId })
                    .ToListAsync();

                // Trừ sĩ số còn lại + gom examId theo môn
                foreach (var x in createdNow)
                {
                    remain[x.SubjectId] = Math.Max(0, remain[x.SubjectId] - cap);
                    if (!examsBySubject.TryGetValue(x.SubjectId, out var list))
                        examsBySubject[x.SubjectId] = list = new List<int>();
                    list.Add(x.ExamId);
                }

                tick++; // sang ca kế
            }

            // Gán sinh viên vào ca (mỗi phòng tối đa 'cap' SV)
            var students = await _db.Set<StudentSubject>()
                .OrderBy(x => x.SubjectId).ThenBy(x => x.StudentId)
                .ToListAsync();

            var counter = new Dictionary<int, int>(); // subjectId -> đã gán bao nhiêu SV
            foreach (var ss in students)
            {
                counter.TryGetValue(ss.SubjectId, out var r);
                int bucket = r / cap; // mỗi 'cap' SV chuyển sang exam tiếp theo của môn đó
                var list = examsBySubject[ss.SubjectId];
                if (bucket >= list.Count) bucket = list.Count - 1;

                _db.StudentExams.Add(new StudentExam
                {
                    StudentId = ss.StudentId,
                    ExamId    = list[bucket]
                });
                counter[ss.SubjectId] = r + 1;
            }
            await _db.SaveChangesAsync();
        }
    }
}
