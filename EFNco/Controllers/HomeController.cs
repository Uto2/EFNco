using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using EFNco.Models;

namespace EFNco.Controllers
{
    public class HomeController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            // Unauthenticated users see the public landing page
            if (User.Identity == null || !User.Identity.IsAuthenticated)
                return View("Landing");

            // Authenticated users see the dashboard
            var user = await _userManager.GetUserAsync(User);
            var roles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();
            ViewBag.UserName = user?.FullName ?? "User";
            ViewBag.Role = roles.FirstOrDefault() ?? "User";
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
