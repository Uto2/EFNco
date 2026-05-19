using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EFNco.Data;
using EFNco.Models;

namespace EFNco.Controllers
{
    public class HomeController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _db;

        public HomeController(UserManager<ApplicationUser> userManager, ApplicationDbContext db)
        {
            _userManager = userManager;
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
                return View("Landing");

            if (User.IsInRole("Guard"))
                return RedirectToAction("Scan", "Gate");
            
            if (!User.IsInRole("Admin") && !User.IsInRole("Guard"))
                return RedirectToAction("MyPermits", "Permit");

            var user = await _userManager.GetUserAsync(User);
            var roles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();

            // Instruction 1: Pass CurrentUserId to ViewBag for Layout/Notifications
            ViewBag.CurrentUserId = user?.Id;

            ViewBag.UserName = user?.FullName ?? "User";
            ViewBag.Role = roles.FirstOrDefault() ?? "User";

            // Dashboard stats
            ViewBag.ActivePermits = await _db.ParkingPermits.CountAsync(p => p.Status == PermitStatus.Approved);
            ViewBag.PendingPermits = await _db.ParkingPermits.CountAsync(p => p.Status == PermitStatus.Pending);
            ViewBag.TotalUsers = await _userManager.Users.CountAsync();
            ViewBag.TodayEntries = await _db.EntryExitLogs.CountAsync(l => l.Timestamp.Date == DateTime.Today && l.Action == LogAction.Entry);

            ViewBag.CurrentlyInside = await _db.EntryExitLogs
                .GroupBy(l => l.PermitId)
                .Select(g => g.OrderByDescending(l => l.Timestamp).First())
                .CountAsync(l => l.Action == LogAction.Entry);

            // Instruction 5 & 6: Update Violations count for the dashboard card
            ViewBag.TotalViolations = await _db.Violations.CountAsync(v => v.Status == ViolationStatus.Unpaid);

            return View();
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}