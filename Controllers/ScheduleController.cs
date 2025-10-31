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
            var weeks   = int.Parse(_cfg["ExamSchedule:Weeks"] ?? "2");
            var numDays = weeks * 7;

            // Lấy danh sách môn, phòng
            var subjectIds = _context.Subjects.Select(s => s.SubjectId).ToList();
            var roomIds    = _context.Rooms.Select(r => r.RoomId).ToList();

            //  tạo FitnessCalculator 
            var fitness = new FitnessCalculator(_context, _cfg);

            // chạy GA
            var ga = new GeneticAlgorithm(
                fitness,
                subjectIds,
                roomIds,
                numDays,
                populationSize: 120,
                generations: 400,
                mutationRate: 0.10
            );

            var best = ga.Run();

            // xuất exam_plan 
            ga.SavePlan(_context, best);

            TempData["msg"] = $"GA done. Fitness = {best.Fitness}";
            return RedirectToAction(nameof(Plan));
        }
    }
}
