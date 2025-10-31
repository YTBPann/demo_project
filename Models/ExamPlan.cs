using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenIDApp.Models
{
    [Table("exam_plan")]
    public class ExamPlan
    {
        [Key] public int PlanId { get; set; }

        // Mỗi môn 1 lịch thi → UNIQUE
        [Required] public int SubjectId { get; set; }

        // 0..NumDays-1 và 0..3 (4 ca/ngày)
        [Required] public int DayIndex { get; set; }
        [Required] public int SlotId { get; set; }

        [Required] public int RoomId { get; set; }

        // Optional navs
        public Subject? Subject { get; set; }
        public Room? Room { get; set; }
    }
}
