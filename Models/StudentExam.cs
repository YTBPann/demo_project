using System.ComponentModel.DataAnnotations.Schema;

namespace OpenIDApp.Models
{
    [Table("student_exams")]
    public class StudentExam
    {
        [Column("student_id")]
        public int StudentId { get; set; }

        [Column("exam_id")]
        public int ExamId { get; set; }

        public Student Student { get; set; } = null!;

        public Exam Exam { get; set; } = null!;
    }
}