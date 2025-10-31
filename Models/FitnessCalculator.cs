using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenIDApp.Models
{
    /// <summary>
    /// Chấm điểm 1 nhiễm sắc thể lịch thi theo ràng buộc:
    /// - Phòng ≤ 45 (hard), ≤ capacity (soft)
    /// - Không trùng lịch sinh viên, giảng viên (cùng day,slot)
    /// - Giới hạn số phòng dùng đồng thời trong 1 slot
    /// - Cân bằng theo ngày
    /// </summary>
    public class FitnessCalculator
    {
        public int RoomHardCap { get; set; } = 45;
        public double PenaltyOverCapacity { get; set; } = 10;
        public double PenaltyStudentOverlap { get; set; } = 20;
        public double PenaltyTeacherOverlap { get; set; } = 15;
        public int MaxRoomsPerSlot { get; set; } = 4;
        public double PenaltyRoomsPerSlot { get; set; } = 8;
        public double PenaltyDayImbalance { get; set; } = 1.0;

        private readonly IDictionary<int,int> _roomCapacityById;            // room_id -> cap
        private readonly IDictionary<int,int> _teacherBySubject;            // subject_id -> teacher_id
        private readonly IDictionary<int,HashSet<int>> _studentsBySubject;  // subject_id -> set(student_id)
        private readonly Func<int,int,DateTime>? _slotStartUtc;             // (day,slot) -> startUtc (optional)
        private readonly Func<DateTime,int>? _dayIndexOf;                   // fallback nếu gene chỉ có StartTime
        private readonly Func<DateTime,int>? _slotIdOf;                     // fallback nếu gene chỉ có StartTime

        public FitnessCalculator(
            IDictionary<int,int> roomCapacityById,
            IDictionary<int,int> teacherBySubject,
            IDictionary<int,HashSet<int>> studentsBySubject,
            Func<int,int,DateTime>? slotStartUtc = null,
            Func<DateTime,int>? dayIndexOf = null,
            Func<DateTime,int>? slotIdOf = null)
        {
            _roomCapacityById  = roomCapacityById;
            _teacherBySubject  = teacherBySubject;
            _studentsBySubject = studentsBySubject;
            _slotStartUtc      = slotStartUtc;
            _dayIndexOf        = dayIndexOf;
            _slotIdOf          = slotIdOf;
        }

        private static (int day, int slot) ResolveSlot(ExamGene g, Func<DateTime,int>? dayOf, Func<DateTime,int>? slotOf)
        {
            // Ưu tiên DayIndex/SlotId nếu gene đã có (bạn có thể thêm 2 property vào ExamGene)
            if (g.DayIndex >= 0 && g.SlotId >= 0) return (g.DayIndex, g.SlotId);

            // Fallback: suy ra từ StartTime (nếu project cũ còn dùng)
            if (dayOf != null && slotOf != null && g.StartTime != default) 
                return (dayOf(g.StartTime), slotOf(g.StartTime));

            // Không có thông tin → gom vào day 0 / slot 0 (sẽ bị phạt nếu chồng chéo)
            return (0, 0);
        }

        public double Evaluate(ExamChromosome chromo)
        {
            double score = 10000.0;

            // Gom theo (day,slot,room)
            var bySlotRoom = chromo.Genes.GroupBy(g => {
                var (d, s) = ResolveSlot(g, _dayIndexOf, _slotIdOf);
                return new { Day = d, Slot = s, g.RoomId };
            });

            // 1) Phòng quá tải
            foreach (var grp in bySlotRoom)
            {
                int roomId = grp.Key.RoomId;
                int cap = _roomCapacityById.TryGetValue(roomId, out var c) ? c : 45;

                var students = new HashSet<int>();
                foreach (var gene in grp)
                    if (_studentsBySubject.TryGetValue(gene.SubjectId, out var set))
                        foreach (var sid in set) students.Add(sid);

                int n = students.Count;
                if (n > RoomHardCap)      score -= (n - RoomHardCap) * PenaltyOverCapacity;
                else if (n > cap)         score -= (n - cap) * (PenaltyOverCapacity * 0.5);
            }

            // 2) Trùng lịch SV + 3) Trùng lịch GV + 4) Số phòng/slot
            var bySlot = chromo.Genes.GroupBy(g => {
                var (d, s) = ResolveSlot(g, _dayIndexOf, _slotIdOf);
                return new { Day = d, Slot = s };
            });

            foreach (var slot in bySlot)
            {
                // SV overlap
                var seenStu = new Dictionary<int,int>();
                foreach (var gene in slot)
                {
                    if (!_studentsBySubject.TryGetValue(gene.SubjectId, out var set)) continue;
                    foreach (var sid in set)
                        seenStu[sid] = seenStu.TryGetValue(sid, out var c) ? c + 1 : 1;
                }
                foreach (var c in seenStu.Values) if (c > 1) score -= (c - 1) * PenaltyStudentOverlap;

                // GV overlap
                var seenTea = new Dictionary<int,int>();
                foreach (var gene in slot)
                    if (_teacherBySubject.TryGetValue(gene.SubjectId, out var t))
                        seenTea[t] = seenTea.TryGetValue(t, out var c) ? c + 1 : 1;
                foreach (var c in seenTea.Values) if (c > 1) score -= (c - 1) * PenaltyTeacherOverlap;

                // Rooms used in this slot
                int roomsUsed = slot.Select(g => g.RoomId).Distinct().Count();
                if (roomsUsed > MaxRoomsPerSlot)
                    score -= (roomsUsed - MaxRoomsPerSlot) * PenaltyRoomsPerSlot;
            }

            // 5) Cân bằng giữa các ngày
            var dayCounts = chromo.Genes.GroupBy(g => ResolveSlot(g, _dayIndexOf, _slotIdOf).Item1)
                                        .ToDictionary(g => g.Key, g => g.Count());
            if (dayCounts.Count > 0)
            {
                double mean = dayCounts.Values.Average();
                double var  = dayCounts.Values.Select(v => (v - mean) * (v - mean)).Average();
                score -= var * PenaltyDayImbalance;
            }

            return score;
        }
    }
}
