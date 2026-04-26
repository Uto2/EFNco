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

            var user = await _userManager.GetUserAsync(User);
            var roles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();
            ViewBag.UserName = user?.FullName ?? "User";
            ViewBag.Role = roles.FirstOrDefault() ?? "User";

            // Dashboard stats
            ViewBag.ActivePermits   = await _db.ParkingPermits.CountAsync(p => p.Status == PermitStatus.Approved);
            ViewBag.PendingPermits  = await _db.ParkingPermits.CountAsync(p => p.Status == PermitStatus.Pending);
            ViewBag.TotalUsers      = await _userManager.Users.CountAsync();
            ViewBag.TotalViolations = 0; // Sprint 6

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
