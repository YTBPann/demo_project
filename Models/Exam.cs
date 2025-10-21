using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenIDApp.Models
{
    [Table("exams")]
    public class Exam
    {
        [Column("exam_id")]
        public int ExamId { get; set; }

        [Column("subject_id")]
        public int SubjectId { get; set; }

        [Column("room_id")]
        public int RoomId { get; set; }

        [Column("date")]
        public DateTime Date { get; set; }

        [Column("duration_minutes")]
        public int DurationMinutes { get; set; }

        public Subject Subject { get; set; } = null!;

        public Room Room { get; set; } = null!;

        public ICollection<StudentExam> StudentExams { get; set; } = new List<StudentExam>();
    }
}