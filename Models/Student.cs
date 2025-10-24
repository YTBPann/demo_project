using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenIDApp.Models
{
    [Table("students")]
    public class Student
    {
        [Column("student_id")]
        public int StudentId { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [ForeignKey("UserId")] 
        public User UserProfile { get; set; } = null!;

        [Column("student_code")]
        public string? StudentCode { get; set; }

        [Column("class_name")]
        public string? ClassName { get; set; }

        [Column("major")]
        public string? Major { get; set; }

        [Column("year")]
        public int? Year { get; set; }

        public ICollection<StudentExam> StudentExams { get; set; } = new List<StudentExam>();
    }
}
