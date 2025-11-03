using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenIDApp.Models
{
    [Table("exam_plan_rooms")]
    public class ExamPlanRoom
    {
        [Key] public int Id { get; set; }

        [Required] public int SubjectId { get; set; }
        [Required] public int DayIndex  { get; set; } // 0..N-1
        [Required] public int SlotId    { get; set; } // 0..3
        [Required] public int RoomId    { get; set; }
        [Required] public int Seats     { get; set; }

        public Subject? Subject { get; set; }
        public Room? Room { get; set; }
    }
}
