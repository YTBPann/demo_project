using System;
using System.Collections.Generic;
using System.Linq;
using OpenIDApp.Models;


namespace OpenIDApp.Services
{
    /// <summary>
    /// Dịch vụ chính để xử lý thuật toán di truyền tối ưu lịch thi
    /// </summary>
    public class GeneticAlgorithmService
    {
        private readonly Random _random = new Random();

        // Cấu hình thuật toán
        private const int PopulationSize = 30;     // số cá thể trong quần thể
        private const int MaxGenerations = 100;    // số thế hệ lặp
        private const double MutationRate = 0.1;   // tỉ lệ đột biến

        private readonly List<Room> _rooms;
        private readonly List<Subject> _subjects;

        public GeneticAlgorithmService(List<Room> rooms, List<Subject> subjects)
        {
            _rooms = rooms;
            _subjects = subjects;
        }

        /// <summary>
        /// Hàm khởi tạo quần thể ngẫu nhiên
        /// </summary>
        private List<ExamChromosome> InitializePopulation()
        {
            var population = new List<ExamChromosome>();
            for (int i = 0; i < PopulationSize; i++)
            {
                var chromosome = new ExamChromosome();
                foreach (var subject in _subjects)
                {
                    var room = _rooms[_random.Next(_rooms.Count)];
                    var dayOffset = _random.Next(0, 5); // 5 ngày thi
                    var startTime = DateTime.Today.AddDays(dayOffset).AddHours(8 + _random.Next(0, 8)); // từ 8h đến 16h

                    chromosome.Genes.Add(new ExamGene
                    {
                        SubjectId = subject.SubjectId,
                        RoomId = room.RoomId,
                        StartTime = startTime,
                        DurationMinutes = 90
                    });
                }
                population.Add(chromosome);
            }
            return population;
        }

        /// <summary>
        /// Hàm tính fitness cho từng cá thể
        /// </summary>
        private double CalculateFitness(ExamChromosome chromosome)
        {
            // Fitness càng cao càng tốt — ta đánh giá dựa trên xung đột phòng
            double score = 100.0;
            var groups = chromosome.Genes.GroupBy(g => new { g.RoomId, g.StartTime }).ToList();

            foreach (var group in groups)
            {
                if (group.Count() > 1)
                {
                    // nếu 2 môn trùng giờ cùng phòng → trừ điểm
                    score -= (group.Count() - 1) * 10;
                }
            }

            return Math.Max(0, score);
        }

        /// <summary>
        /// Chọn cá thể tốt nhất trong quần thể
        /// </summary>
        private ExamChromosome SelectBest(List<ExamChromosome> population)
        {
            foreach (var c in population)
                c.Fitness = CalculateFitness(c);

            return population.OrderByDescending(c => c.Fitness).First();
        }

        /// <summary>
        /// Hàm chạy thuật toán di truyền đơn giản (phiên bản đầu)
        /// </summary>
        public ExamChromosome Run()
        {
            var population = InitializePopulation();

            for (int gen = 0; gen < MaxGenerations; gen++)
            {
                foreach (var chromosome in population)
                {
                    chromosome.Fitness = CalculateFitness(chromosome);
                }

                // Chọn ra cá thể tốt nhất
                var best = population.OrderByDescending(c => c.Fitness).First();

                // In ra console (chỉ để test)
                Console.WriteLine($"Thế hệ {gen + 1}: Fitness = {best.Fitness}");

                // Nếu đạt đủ điểm tốt, dừng sớm
                if (best.Fitness >= 100)
                    return best;

                // Đột biến nhẹ
                foreach (var c in population)
                {
                    if (_random.NextDouble() < MutationRate)
                    {
                        var gene = c.Genes[_random.Next(c.Genes.Count)];
                        gene.RoomId = _rooms[_random.Next(_rooms.Count)].RoomId;
                        gene.StartTime = gene.StartTime.AddHours(_random.Next(-1, 2));
                    }
                }
            }

            // Trả về cá thể tốt nhất cuối cùng
            return SelectBest(population);
        }
    }
}
