// Controllers/ScheduleController.cs
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OpenIDApp.Models;

namespace OpenIDApp.Controllers
{
    [Authorize]
    public class ScheduleController : Controller
    {
        private readonly OpenIDContext _context;
        private readonly IConfiguration _cfg;

        public ScheduleController(OpenIDContext context, IConfiguration cfg)
        {
            _context = context;
            _cfg = cfg;
        }

        [Authorize(Roles = "student")]
        public async Task<IActionResult> Student()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value
                        ?? User.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
            var displayName = User.Identity?.Name;

            var user = await _context.Users.FirstOrDefaultAsync(u =>
                (email != null && u.Email == email) ||
                (email == null && displayName != null && u.Name == displayName));

            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy thông tin người dùng hiện tại.";
                return RedirectToAction("Welcome", "Home");
            }

            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == user.Id);
            if (student == null)
            {
                TempData["Error"] = "Tài khoản này không gắn với hồ sơ sinh viên.";
                return RedirectToAction("Welcome", "Home");
            }

            var exams = await _context.StudentExams
                .Where(se => se.StudentId == student.StudentId)
                .Include(se => se.Exam).ThenInclude(e => e.Subject)
                .Include(se => se.Exam).ThenInclude(e => e.Room)
                .Select(se => se.Exam)
                .OrderBy(e => e.Date)
                .ToListAsync();

            return View(exams);
        }

        [Authorize(Roles = "teacher")]
        public async Task<IActionResult> Teacher()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value
                        ?? User.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
            var displayName = User.Identity?.Name;

            var user = await _context.Users.FirstOrDefaultAsync(u =>
                (email != null && u.Email == email) ||
                (email == null && displayName != null && u.Name == displayName));

            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy thông tin người dùng hiện tại.";
                return RedirectToAction("Welcome", "Home");
            }

            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == user.Id);
            if (teacher == null)
            {
                TempData["Error"] = "Tài khoản này không gắn với hồ sơ giáo viên.";
                return RedirectToAction("Welcome", "Home");
            }

            var exams = await _context.Exams
                .Include(e => e.Subject)
                .Include(e => e.Room)
                .Where(e => e.Subject.TeacherId == teacher.TeacherId)
                .OrderBy(e => e.Date)
                .ToListAsync();

            return View(exams);
        }

        [Authorize(Roles = "admin")]
        [HttpGet]
        public async Task<IActionResult> Plan()
        {
            var plans = await _context.ExamPlans
                .Include(p => p.Subject)
                .Include(p => p.Room)
                .OrderBy(p => p.DayIndex).ThenBy(p => p.SlotId)
                .ToListAsync();

            return View(plans);
        }

        [Authorize(Roles = "admin")]
        [HttpPost]
        public IActionResult RunGA()
        {
            var roomCap = _context.Rooms.AsNoTracking()
                .ToDictionary(r => r.RoomId, r => r.Capacity);

            var teacherBySubj = _context.Subjects.AsNoTracking()
                .Where(s => s.TeacherId != null)
                .ToDictionary(s => s.SubjectId, s => s.TeacherId!.Value);

            var studentsBySubj = _context.StudentExams.AsNoTracking()
                .Include(se => se.Exam).ThenInclude(e => e.Subject)
                .GroupBy(se => se.Exam.SubjectId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.StudentId).ToHashSet()
                );

            int weeks = _cfg.GetValue<int>("ExamSchedule:Weeks", 2);
            int numDays = weeks * 7;

            var tzId = _cfg.GetValue<string>("ExamSchedule:TimeZone") ?? "Asia/Ho_Chi_Minh";
            var tz = TimeZoneInfo.FindSystemTimeZoneById(tzId);

            var baseLocalStr = _cfg.GetValue<string>("ExamSchedule:BaseDateLocal") ?? "2025-11-10";
            var baseLocal = DateTime.Parse(baseLocalStr);
            var baseUtc = TimeZoneInfo.ConvertTimeToUtc(baseLocal, tz);

            var slotPlan = new SlotPlan(baseUtc, numDays, tz);

            Func<DateTime, int> dayOf  = dt => slotPlan.GetDayIndexFromUtc(dt);
            Func<DateTime, int> slotOf = dt => 0;

            var fitness = new FitnessCalculator(roomCap, teacherBySubj, studentsBySubj, null, dayOf, slotOf)
            {
                MaxRoomsPerSlot = _cfg.GetValue<int>("ExamSchedule:MaxRoomsPerSlot", 4)
            };

            var subjectIds = _context.Subjects.AsNoTracking()
                .Select(s => s.SubjectId).OrderBy(x => x).ToList();

            var roomIds = _context.Rooms.AsNoTracking()
                .Select(r => r.RoomId).OrderBy(x => x).ToList();

            var ga = new GeneticAlgorithm(fitness, subjectIds, roomIds, numDays,
                populationSize: 80, generations: 300, mutationRate: 0.08);

            var best = ga.Run();
            ga.SavePlan(_context, best);

            return Ok(new { best.Fitness, Genes = best.Genes.Count });
        }

        [Authorize(Roles = "admin")]
        [HttpPost]
        public IActionResult Publish()
        {
            int weeks = _cfg.GetValue<int>("ExamSchedule:Weeks", 2);
            int numDays = weeks * 7;

            var tzId = _cfg.GetValue<string>("ExamSchedule:TimeZone") ?? "Asia/Ho_Chi_Minh";
            var tz = TimeZoneInfo.FindSystemTimeZoneById(tzId);

            var baseLocalStr = _cfg.GetValue<string>("ExamSchedule:BaseDateLocal") ?? "2025-11-10";
            var baseLocal = DateTime.Parse(baseLocalStr);
            var baseUtc = TimeZoneInfo.ConvertTimeToUtc(baseLocal, tz);

            int duration = _cfg.GetValue<int>("ExamSchedule:DurationMinutes", 90);

            var slotPlan = new SlotPlan(baseUtc, numDays, tz);
            var publisher = new SchedulePublisher(slotPlan, duration);

            var plans = _context.ExamPlans.AsNoTracking().ToList();

            _context.Database.ExecuteSqlRaw(@"
                DELETE e FROM exams e
                JOIN subjects s ON s.subject_id = e.subject_id
            ");

            foreach (var p in plans)
            {
                var startUtc = publisher.ToStartUtc(p.DayIndex, p.SlotId);
                var endUtc   = publisher.ToEndUtc(p.DayIndex, p.SlotId);
                int minutes  = (int)(endUtc - startUtc).TotalMinutes;

                _context.Database.ExecuteSqlRaw(@"
                    INSERT INTO exams (subject_id, room_id, date, duration_minutes)
                    VALUES ({0}, {1}, {2}, {3})
                ", p.SubjectId, p.RoomId, startUtc, minutes);
            }

            return Ok(new { Published = plans.Count });
        }
    }
}
