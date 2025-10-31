// Models/GeneticAlgorithm.cs
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace OpenIDApp.Models
{
    public class GeneticAlgorithm
    {
        private readonly Random _rnd = new Random();
        private readonly FitnessCalculator _fitness;
        private readonly IList<int> _subjectIds;
        private readonly IList<int> _roomIds;
        private readonly int _numDays;
        private readonly int _populationSize;
        private readonly int _generations;
        private readonly double _mutationRate;

        public GeneticAlgorithm(
            FitnessCalculator fitness,
            IEnumerable<int> subjectIds,
            IEnumerable<int> roomIds,
            int numDays,
            int populationSize = 80,
            int generations = 300,
            double mutationRate = 0.08)
        {
            _fitness = fitness;
            _subjectIds = subjectIds.ToList();
            _roomIds = roomIds.ToList();
            _numDays = numDays;
            _populationSize = populationSize;
            _generations = generations;
            _mutationRate = mutationRate;
        }

        public ExamChromosome Run()
        {
            var pop = InitPopulation();
            Evaluate(pop);

            for (int gen = 0; gen < _generations; gen++)
            {
                var next = new List<ExamChromosome>(_populationSize);

                var elite = pop.OrderByDescending(c => c.Fitness).First().Clone();
                next.Add(elite);

                while (next.Count < _populationSize)
                {
                    var p1 = Tournament(pop);
                    var p2 = Tournament(pop);
                    var child = Crossover(p1, p2);
                    Mutate(child);
                    next.Add(child);
                }

                Evaluate(next);
                pop = next;
            }

            return pop.OrderByDescending(c => c.Fitness).First();
        }

        public void SavePlan(OpenIDContext db, ExamChromosome best)
        {
            foreach (var g in best.Genes)
            {
                db.Database.ExecuteSqlRaw(@"
                    INSERT INTO exam_plan (subject_id, day_index, slot_id, room_id)
                    VALUES ({0}, {1}, {2}, {3})
                    ON DUPLICATE KEY UPDATE
                    day_index=VALUES(day_index),
                    slot_id  =VALUES(slot_id),
                    room_id  =VALUES(room_id);
                ", g.SubjectId, g.DayIndex, g.SlotId, g.RoomId);
            }
        }

        //  GA core 
        private List<ExamChromosome> InitPopulation()
        {
            var pop = new List<ExamChromosome>(_populationSize);
            for (int i = 0; i < _populationSize; i++)
                pop.Add(RandomChromosome());
            return pop;
        }

        private ExamChromosome RandomChromosome()
        {
            var c = new ExamChromosome();
            foreach (var sid in _subjectIds)
            {
                c.Genes.Add(new ExamGene {
                    SubjectId = sid,
                    RoomId    = _roomIds[_rnd.Next(_roomIds.Count)],
                    DayIndex  = _rnd.Next(_numDays), // 0.._numDays-1
                    SlotId    = _rnd.Next(4),        // 4 ca/ngày
                    DurationMinutes = 90
                });
            }
            return c;
        }


        private void Evaluate(List<ExamChromosome> pop)
        {
            foreach (var c in pop)
                c.Fitness = _fitness.Evaluate(c);
        }

        private ExamChromosome Tournament(List<ExamChromosome> pop, int k = 4)
        {
            ExamChromosome? best = null;
            for (int i = 0; i < k; i++)
            {
                var cand = pop[_rnd.Next(pop.Count)];
                if (best == null || cand.Fitness > best.Fitness) best = cand;
            }
            return best!.Clone();
        }

        private ExamChromosome Crossover(ExamChromosome a, ExamChromosome b)
        {
            var child = new ExamChromosome();
            for (int i = 0; i < a.Genes.Count; i++)
            {
                var src = (_rnd.NextDouble() < 0.5) ? a.Genes[i] : b.Genes[i];
                child.Genes.Add(src.Clone());
            }
            return child;
        }

        private void Mutate(ExamChromosome c)
        {
            foreach (var g in c.Genes)
            {
                if (_rnd.NextDouble() < _mutationRate)
                {
                    switch (_rnd.Next(3))
                    {
                        case 0: g.RoomId   = _roomIds[_rnd.Next(_roomIds.Count)]; break;
                        case 1: g.DayIndex = _rnd.Next(_numDays); break;
                        case 2: g.SlotId   = _rnd.Next(4); break;
                    }
                }
            }
        }
    }
}
