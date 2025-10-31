using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;

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

        // Đẩy exam_plan -> exams (giờ LOCAL, KHÔNG UTC)
        public async Task<int> PublishAsync()
        {
            // Đọc cấu hình
            var baseDateLocal = DateTime.Parse(_cfg["ExamSchedule:BaseDateLocal"] ?? "2025-11-10").Date;
            var duration      = int.Parse(_cfg["ExamSchedule:DurationMinutes"] ?? "90");

            // (tuỳ chọn) Xoá sạch exams trước khi publish
            await _db.Database.ExecuteSqlRawAsync("DELETE FROM exams;");

            // LẤY TỪ DbSet ĐÚNG TÊN: ExamPlans (số nhiều)
            var plans = await _db.ExamPlans
                .AsNoTracking()
                .OrderBy(p => p.SubjectId)
                .ToListAsync();

            if (plans.Count == 0) return 0;

            // Phòng mặc định nếu plan chưa có room
            var defaultRoomId = await _db.Rooms
                .OrderBy(r => r.RoomId)
                .Select(r => r.RoomId)
                .FirstAsync();

            foreach (var p in plans)
            {
                // 
                var slotId = Math.Max(0, Math.Min(TimeSlot.SlotsPerDay - 1, p.SlotId));
                var (startLocal, endLocal) = TimeSlot.GetSlotLocalTime(slotId);

                // 
                var dtLocal = baseDateLocal.AddDays(Math.Max(0, p.DayIndex)).Add(startLocal);
                dtLocal = DateTime.SpecifyKind(dtLocal, DateTimeKind.Unspecified);

                //
                var roomId = (p.RoomId > 0) ? p.RoomId : defaultRoomId;

                _db.Exams.Add(new Exam
                {
                    SubjectId       = p.SubjectId,
                    RoomId          = roomId,
                    Date            = dtLocal,
                    DurationMinutes = (int)(endLocal - startLocal).TotalMinutes
                });
            }
            return await _db.SaveChangesAsync();
        }
    }
}
