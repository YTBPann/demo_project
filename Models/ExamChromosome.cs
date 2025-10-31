using System;
using System.Collections.Generic;

namespace OpenIDApp.Models
{
    public class ExamChromosome
    {
        public List<ExamGene> Genes { get; set; } = new List<ExamGene>();
        public double Fitness { get; set; } = 0.0;  // Độ thích nghi

        public ExamChromosome Clone()
        {
            var clone = new ExamChromosome();
            foreach (var gene in Genes)
            {
                clone.Genes.Add(gene.Clone());
            }
            clone.Fitness = this.Fitness;
            return clone;
        }
    }

    // Một gen đại diện cho 1 môn thi
    public partial class ExamGene
    {
        public int SubjectId { get; set; }
        public int RoomId { get; set; }

        // Nếu file cũ còn StartTime/DurationMinutes cứ giữ, GA sẽ không dùng StartTime nữa
        public DateTime StartTime { get; set; }
        public int DurationMinutes { get; set; } = 90;

        // MỚI: làm việc theo 4 ca/ngày
        public int DayIndex { get; set; } = -1;  // 0..NumDays-1
        public int SlotId  { get; set; } = -1;   // 0..3

        public ExamGene Clone()
        {
            return new ExamGene
            {
                SubjectId = this.SubjectId,
                RoomId = this.RoomId,
                StartTime = this.StartTime,
                DurationMinutes = this.DurationMinutes,
                DayIndex = this.DayIndex,   // ⬅️ nhớ copy
                SlotId  = this.SlotId       // ⬅️ nhớ copy
            };
        }
    }
}
