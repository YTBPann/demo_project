using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenIDApp.Models
{
    [Table("teachers")]
    public class Teacher
    {
        [Column("teacher_id")]
        public int TeacherId { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("teacher_code")]
        public string? TeacherCode { get; set; }

        [Column("department")]
        public string? Department { get; set; }

        public User? User { get; set; }

        public ICollection<Subject> Subjects { get; set; } = new List<Subject>();
    }
}