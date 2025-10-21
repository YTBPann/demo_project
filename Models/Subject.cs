using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenIDApp.Models
{
    [Table("subjects")]
    public class Subject
    {
        [Column("subject_id")]
        public int SubjectId { get; set; }

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("teacher_id")]
        public int? TeacherId { get; set; }

        public Teacher? Teacher { get; set; }

        public ICollection<Exam> Exams { get; set; } = new List<Exam>();
    }
}