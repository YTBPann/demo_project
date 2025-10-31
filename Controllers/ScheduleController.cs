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
            // 
            var email = User.FindFirst(ClaimTypes.Email)?.Value
                        ?? User.Claims.FirstOrDefault(c => c.Type == "email")?.Value;

            // 
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy người dùng hiện tại.";
                return RedirectToAction("Welcome", "Home");
            }
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == user.Id);
            if (student == null)
            {
                TempData["Error"] = "Tài khoản này chưa gắn hồ sơ sinh viên.";
                return RedirectToAction("Welcome", "Home");
            }

            // 
            var myExams = await _context.StudentExams
                .Where(se => se.StudentId == student.StudentId)
                .Include(se => se.Exam).ThenInclude(e => e.Subject)
                .Include(se => se.Exam).ThenInclude(e => e.Room)
                .Select(se => se.Exam!)
                .OrderBy(e => e.Date)
                .ToListAsync();

            return View(myExams);
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
            // 
            var weeks   = int.Parse(_cfg["ExamSchedule:Weeks"] ?? "2");
            var numDays = weeks * 7;

            // 
            var roomCap = _context.Rooms
                .Select(r => new { r.RoomId, r.Capacity })
                .ToDictionary(x => x.RoomId, x => x.Capacity);

            // 
            var teacherBySubj = _context.Subjects
                .Select(s => new { s.SubjectId, TeacherId = (int?)s.TeacherId })
                .ToDictionary(x => x.SubjectId, x => x.TeacherId ?? 0);

            // 
            var studentsBySubject = _context.StudentExams
                .Include(se => se.Exam)
                .Where(se => se.Exam != null)
                .AsEnumerable()
                .GroupBy(se => se.Exam!.SubjectId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(se => se.StudentId).ToHashSet()
                );

            // 
            var fitness = new FitnessCalculator(
                roomCap,
                teacherBySubj,
                studentsBySubject,
                null, null, null // không cần map thời gian ở bước GA
            );

            // 
            var subjectIds = _context.Subjects.Select(s => s.SubjectId).ToList();
            var roomIds    = _context.Rooms.Select(r => r.RoomId).ToList();

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

            // 
            ga.SavePlan(_context, best);

            // 
            var planRows = _context.Database
                .ExecuteSqlRaw("SELECT 1"); 

            TempData["msg"] = $"GA saved {best.Genes.Count} subjects to exam_plan.";
            return RedirectToAction(nameof(Plan));
        }
        [Authorize(Roles = "admin")]
        [HttpPost]
        public async Task<IActionResult> Publish()
        {
            // 
            var weeks    = int.Parse(_cfg["ExamSchedule:Weeks"] ?? "2");
            var numDays  = Math.Max(1, weeks * 7);
            var baseLocal = DateTime.Parse(_cfg["ExamSchedule:BaseDateLocal"] ?? "2025-11-10").Date;

            // 
            var plans = await _context.ExamPlans.AsNoTracking().ToListAsync();
            if (plans.Count == 0)
            {
                TempData["Error"] = "exam_plan đang rỗng — hãy Run GA trước.";
                return RedirectToAction(nameof(Plan));
            }

            // 
            var subjectIds = plans.Select(p => p.SubjectId).ToList();
            var oldExams   = _context.Exams.Where(e => subjectIds.Contains(e.SubjectId));
            _context.Exams.RemoveRange(oldExams);
            await _context.SaveChangesAsync();

            // 
            var firstRoomId = await _context.Rooms.Select(r => r.RoomId).OrderBy(id => id).FirstAsync();

            foreach (var p in plans)
            {
                var dayIdx = p.DayIndex < 0 ? 0 : p.DayIndex;
                var slotId = p.SlotId   < 0 ? 0 : Math.Min(TimeSlot.SlotsPerDay - 1, p.SlotId);

                var (startLocal, endLocal) = TimeSlot.GetSlotLocalTime(slotId);
                var start   = baseLocal.AddDays(dayIdx).Add(startLocal);               // <-- LOCAL
                var minutes = (int)(endLocal - startLocal).TotalMinutes;

                var roomId = p.RoomId > 0 ? p.RoomId : firstRoomId;

                _context.Exams.Add(new Exam {
                    SubjectId        = p.SubjectId,
                    RoomId           = roomId,
                    Date             = start,      // LƯU LOCAL TIME
                    DurationMinutes  = minutes
                });
            }

            await _context.SaveChangesAsync();
            TempData["msg"] = $"Publish xong {plans.Count} kỳ thi (giờ LOCAL).";
            return RedirectToAction(nameof(Plan));
        }
    }
}
