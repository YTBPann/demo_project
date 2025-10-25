using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIDApp.Services;
using OpenIDApp.Models;

public class AdminController : Controller
{
    private readonly OpenIDApp.Models.OpenIDContext _context;

    public AdminController(OpenIDApp.Models.OpenIDContext context)
    {
        _context = context;
    }

    // Action mặc định của trang Admin
    [HttpGet]
    [Authorize(Roles = "admin")]
    public IActionResult Index()
    {
        return View(); // sẽ load Views/Admin/Index.cshtml
    }

    // Action tối ưu lịch thi
    [HttpGet]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> OptimizeSchedule()
    {
        // Lấy dữ liệu từ DB
        var rooms = await _context.Rooms.AsNoTracking().ToListAsync();
        var subjects = await _context.Subjects.AsNoTracking().ToListAsync();

        if (rooms.Count == 0 || subjects.Count == 0)
        {
            TempData["Error"] = "Thiếu dữ liệu Phòng hoặc Môn học để tối ưu.";
            return RedirectToAction("Index");
        }

        // Chạy GA
        var ga = new GeneticAlgorithmService(rooms, subjects);
        var best = ga.Run();

        // Chuẩn bị dict để view tra cứu tên
        ViewBag.RoomDict = rooms.ToDictionary(r => r.RoomId, r => r.Name);
        ViewBag.SubjectDict = subjects.ToDictionary(s => s.SubjectId, s => s.Name);

        // Gửi kết quả ra View
        return View(best);
    }
}
