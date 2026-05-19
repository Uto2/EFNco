using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EFNco.Data;
using EFNco.Models;

namespace EFNco.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminSettingsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminSettingsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // ── GET: /AdminSettings/DurationLimits ────────────────
        public async Task<IActionResult> DurationLimits()
        {
            var settings = await _db.ParkingDurationSettings
                .OrderBy(s => s.PermitType)
                .ToListAsync();

            // Seed any missing permit types
            var existingTypes = settings.Select(s => s.PermitType).ToHashSet();
            foreach (PermitType pt in Enum.GetValues<PermitType>())
            {
                if (!existingTypes.Contains(pt))
                {
                    _db.ParkingDurationSettings.Add(new ParkingDurationSetting
                    {
                        PermitType  = pt,
                        MaxHours    = 8.0,
                        GraceMinutes = 15,
                        AutoViolation = false
                    });
                }
            }

            if (_db.ChangeTracker.HasChanges())
            {
                await _db.SaveChangesAsync();
                settings = await _db.ParkingDurationSettings
                    .OrderBy(s => s.PermitType)
                    .ToListAsync();
            }

            return View(settings);
        }

        // ── POST: /AdminSettings/DurationLimits ───────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DurationLimits(List<DurationSettingViewModel> models)
        {
            if (!ModelState.IsValid)
                return RedirectToAction("DurationLimits");

            var user = await _userManager.GetUserAsync(User);

            foreach (var m in models)
            {
                var setting = await _db.ParkingDurationSettings
                    .FirstOrDefaultAsync(s => s.Id == m.Id);

                if (setting == null) continue;

                setting.MaxHours      = m.MaxHours;
                setting.GraceMinutes  = m.GraceMinutes;
                setting.AutoViolation = m.AutoViolation;
                setting.UpdatedAt     = DateTime.Now;
                setting.UpdatedByUserId = user?.Id;
            }

            await _db.SaveChangesAsync();

            TempData["Success"] = "Duration limits updated successfully.";
            return RedirectToAction("DurationLimits");
        }
    }
}
