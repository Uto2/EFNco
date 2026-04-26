using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EFNco.Data;
using EFNco.Models;

namespace EFNco.Controllers
{
    [Authorize]
    public class PermitController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public PermitController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // GET: /Permit/MyPermits
        public async Task<IActionResult> MyPermits()
        {
            var userId = _userManager.GetUserId(User);

            var permits = await _db.ParkingPermits
                .Include(p => p.Vehicle)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.AppliedAt)
                .Select(p => new MyPermitViewModel
                {
                    PermitId    = p.Id,
                    VehicleId   = p.VehicleId,
                    PlateNumber = p.Vehicle!.PlateNumber,
                    VehicleDisplay = $"{p.Vehicle.Make} {p.Vehicle.Model} ({p.Vehicle.VehicleType})",
                    PermitType  = p.PermitType,
                    Status      = p.Status,
                    AppliedAt   = p.AppliedAt,
                    ValidFrom   = p.ValidFrom,
                    ValidUntil  = p.ValidUntil,
                    Remarks     = p.Remarks
                })
                .ToListAsync();

            return View(permits);
        }

        // GET: /Permit/Apply
        public async Task<IActionResult> Apply()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            // Check if user already has a pending or approved permit
            bool hasExisting = await _db.ParkingPermits
                .AnyAsync(p => p.UserId == user.Id &&
                          (p.Status == PermitStatus.Pending || p.Status == PermitStatus.Approved));

            if (hasExisting)
            {
                TempData["Error"] = "You already have an active or pending permit. You can only have one permit at a time.";
                return RedirectToAction("MyPermits");
            }

            return View(new ApplyPermitViewModel());
        }

        // POST: /Permit/Apply
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(ApplyPermitViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Collect all validation errors and show them on the form
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                ViewBag.DebugErrors = errors;
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            var userId = user.Id;

            // Double-check no existing active permit
            bool hasExisting = await _db.ParkingPermits
                .AnyAsync(p => p.UserId == userId &&
                          (p.Status == PermitStatus.Pending || p.Status == PermitStatus.Approved));

            if (hasExisting)
            {
                TempData["Error"] = "You already have an active or pending permit.";
                return RedirectToAction("MyPermits");
            }

            // Check plate number not already registered by another user
            var plateExists = await _db.Vehicles
                .AnyAsync(v => v.PlateNumber.ToLower() == model.PlateNumber.ToLower()
                            && v.UserId != userId);

            if (plateExists)
            {
                ModelState.AddModelError("PlateNumber", "This plate number is already registered to another account.");
                return View(model);
            }

            // Create Vehicle
            var vehicle = new Vehicle
            {
                PlateNumber  = model.PlateNumber!.ToUpper().Trim(),
                VehicleType  = model.VehicleType!.Value,
                Make         = model.Make!.Trim(),
                Model        = model.Model!.Trim(),
                Year         = model.Year,
                Color        = model.Color,
                UserId       = userId,
                RegisteredAt = DateTime.UtcNow
            };

            _db.Vehicles.Add(vehicle);
            await _db.SaveChangesAsync();

            // Create Permit
            var permit = new ParkingPermit
            {
                PermitType = model.PermitType!.Value,
                Status     = PermitStatus.Pending,
                AppliedAt  = DateTime.UtcNow,
                Purpose    = model.Purpose,
                VehicleId  = vehicle.Id,
                UserId     = userId
            };

            _db.ParkingPermits.Add(permit);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Permit application submitted successfully! Please wait for admin approval.";
            return RedirectToAction("MyPermits");
        }

        // GET: /Permit/Details/{id}
        public async Task<IActionResult> Details(int id)
        {
            var userId = _userManager.GetUserId(User);

            var permit = await _db.ParkingPermits
                .Include(p => p.Vehicle)
                .Include(p => p.ReviewedBy)
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

            if (permit == null) return NotFound();

            return View(permit);
        }

        // GET: /Permit/Cancel/{id}
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = _userManager.GetUserId(User);

            var permit = await _db.ParkingPermits
                .Include(p => p.Vehicle)
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

            if (permit == null) return NotFound();

            if (permit.Status != PermitStatus.Pending)
            {
                TempData["Error"] = "Only pending permits can be cancelled.";
                return RedirectToAction("MyPermits");
            }

            return View(permit);
        }

        // POST: /Permit/Cancel/{id}
        [HttpPost, ActionName("Cancel")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelConfirmed(int id)
        {
            var userId = _userManager.GetUserId(User);

            var permit = await _db.ParkingPermits
                .Include(p => p.Vehicle)
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

            if (permit == null) return NotFound();

            if (permit.Status != PermitStatus.Pending)
            {
                TempData["Error"] = "Only pending permits can be cancelled.";
                return RedirectToAction("MyPermits");
            }

            // Remove permit and vehicle
            _db.ParkingPermits.Remove(permit);
            _db.Vehicles.Remove(permit.Vehicle!);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Permit application cancelled successfully.";
            return RedirectToAction("MyPermits");
        }
    }
}
