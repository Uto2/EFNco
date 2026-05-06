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

        private static readonly string[] AllowedImageTypes = { ".jpg", ".jpeg", ".png" };
        private static readonly string[] AllowedDocTypes = { ".jpg", ".jpeg", ".png", ".pdf" };
        private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB

        public PermitController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // ── Helper ────────────────────────────────────────────
        private async Task<(byte[]? data, string? fileName, string? contentType, string? error)>
            SaveUploadedFile(IFormFile? file, string[] allowedExtensions, string fieldName)
        {
            if (file == null || file.Length == 0)
                return (null, null, null, null);

            if (file.Length > MaxFileSize)
                return (null, null, null, $"{fieldName} must be under 5 MB.");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
                return (null, null, null, $"{fieldName} must be: {string.Join(", ", allowedExtensions)}.");

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            return (ms.ToArray(), file.FileName, file.ContentType, null);
        }

        // ── GET: /Permit/MyPermits ────────────────────────────
        public async Task<IActionResult> MyPermits()
        {
            var userId = _userManager.GetUserId(User);

            var permits = await _db.ParkingPermits
                .Include(p => p.Vehicle)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.AppliedAt)
                .Select(p => new MyPermitViewModel
                {
                    PermitId = p.Id,
                    VehicleId = p.VehicleId,
                    PlateNumber = p.Vehicle!.PlateNumber,
                    VehicleDisplay = p.Vehicle.Make + " " + p.Vehicle.Model + " (" + p.Vehicle.VehicleType + ")",
                    PermitType = p.PermitType,
                    Status = p.Status,
                    AppliedAt = p.AppliedAt,
                    ValidFrom = p.ValidFrom,
                    ValidUntil = p.ValidUntil,
                    Remarks = p.Remarks,
                    HasLicensePhoto = p.LicensePhotoData != null,
                    HasRegistrationFile = p.RegistrationFileData != null,
                })
                .ToListAsync();

            return View(permits);
        }

        // ── GET: /Permit/Details/{id} ─────────────────────────
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

        // ── GET: /Permit/Apply ────────────────────────────────
        public async Task<IActionResult> Apply()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            bool hasExisting = await _db.ParkingPermits
                .AnyAsync(p => p.UserId == user.Id &&
                          (p.Status == PermitStatus.Pending || p.Status == PermitStatus.Approved));

            if (hasExisting)
            {
                TempData["Error"] = "You already have an active or pending permit.";
                return RedirectToAction("MyPermits");
            }

            return View(new ApplyPermitViewModel());
        }

        // ── POST: /Permit/Apply ───────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(ApplyPermitViewModel model)
        {
            ModelState.Remove("Model");
            ModelState.Remove("LicensePhoto");
            ModelState.Remove("RegistrationFile");

            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            var userId = user.Id;

            bool hasExisting = await _db.ParkingPermits
                .AnyAsync(p => p.UserId == userId &&
                          (p.Status == PermitStatus.Pending || p.Status == PermitStatus.Approved));

            if (hasExisting)
            {
                TempData["Error"] = "You already have an active or pending permit.";
                return RedirectToAction("MyPermits");
            }

            var plateExists = await _db.Vehicles
                .AnyAsync(v => v.PlateNumber.ToLower() == model.PlateNumber!.ToLower()
                            && v.UserId != userId);

            if (plateExists)
            {
                ModelState.AddModelError("PlateNumber", "This plate number is already registered to another account.");
                return View(model);
            }

            var (licenseData, licenseFileName, licenseContentType, licenseError) =
                await SaveUploadedFile(model.LicensePhoto, AllowedImageTypes, "License photo");
            if (licenseError != null) { ModelState.AddModelError("LicensePhoto", licenseError); return View(model); }

            var (regData, regFileName, regContentType, regError) =
                await SaveUploadedFile(model.RegistrationFile, AllowedDocTypes, "Registration file");
            if (regError != null) { ModelState.AddModelError("RegistrationFile", regError); return View(model); }

            var vehicle = new Vehicle
            {
                PlateNumber = model.PlateNumber!.ToUpper().Trim(),
                VehicleType = model.VehicleType!.Value,
                Make = model.Make!.Trim(),
                Model = model.VehicleModel!.Trim(),
                Year = model.Year,
                Color = model.Color,
                UserId = userId,
                RegisteredAt = DateTime.UtcNow
            };

            _db.Vehicles.Add(vehicle);
            await _db.SaveChangesAsync();

            var permit = new ParkingPermit
            {
                PermitType = model.PermitType!.Value,
                Status = PermitStatus.Pending,
                AppliedAt = DateTime.UtcNow,
                Purpose = model.Purpose,
                VehicleId = vehicle.Id,
                UserId = userId,
                LicensePhotoData = licenseData,
                LicensePhotoFileName = licenseFileName,
                LicensePhotoContentType = licenseContentType,
                RegistrationFileData = regData,
                RegistrationFileName = regFileName,
                RegistrationFileContentType = regContentType
            };

            _db.ParkingPermits.Add(permit);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Permit application submitted! Please wait for admin approval.";
            return RedirectToAction("MyPermits");
        }

        // ── GET: /Permit/ViewFile/{id}?type=license|registration
        // Accessible by the permit owner OR any Admin
        public async Task<IActionResult> ViewFile(int id, string type)
        {
            var userId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole("Admin");

            var permit = await _db.ParkingPermits
                .FirstOrDefaultAsync(p => p.Id == id && (isAdmin || p.UserId == userId));

            if (permit == null) return NotFound();

            if (type == "license" && permit.LicensePhotoData != null)
                return File(permit.LicensePhotoData,
                            permit.LicensePhotoContentType ?? "application/octet-stream",
                            permit.LicensePhotoFileName ?? "license");

            if (type == "registration" && permit.RegistrationFileData != null)
                return File(permit.RegistrationFileData,
                            permit.RegistrationFileContentType ?? "application/octet-stream",
                            permit.RegistrationFileName ?? "registration");

            return NotFound();
        }

        // ── GET/POST: /Permit/Cancel/{id} ────────────────────
        [HttpGet]
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

            var vehicle = permit.Vehicle;
            _db.ParkingPermits.Remove(permit);
            if (vehicle != null) _db.Vehicles.Remove(vehicle);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Permit application cancelled.";
            return RedirectToAction("MyPermits");
        }

        // GET: /Permit/QRView/{id}
        public async Task<IActionResult> QRView(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var isAdmin = User.IsInRole("Admin");

            var permit = await _db.ParkingPermits
                .Include(p => p.Vehicle)
                .Include(p => p.Applicant)
                .FirstOrDefaultAsync(p => p.Id == id && (isAdmin || p.UserId == user.Id));

            if (permit == null) return NotFound();

            if (permit.Status != PermitStatus.Approved || permit.QRCodeData == null)
            {
                TempData["Error"] = "QR code is only available for approved permits.";
                return RedirectToAction("Details", new { id });
            }

            return View(permit);
        }

        // GET: /Permit/QRImage/{id}
        public async Task<IActionResult> QRImage(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var isAdmin = User.IsInRole("Admin");

            var permit = await _db.ParkingPermits
                .FirstOrDefaultAsync(p => p.Id == id && (isAdmin || p.UserId == user.Id));

            if (permit == null || permit.QRCodeData == null)
                return NotFound();

            return File(permit.QRCodeData, "image/png");
        }

        // GET: /Permit/PrintPermit/{id}
        public async Task<IActionResult> PrintPermit(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var isAdmin = User.IsInRole("Admin");

            var permit = await _db.ParkingPermits
                .Include(p => p.Vehicle)
                .Include(p => p.Applicant)
                .Include(p => p.ReviewedBy)
                .FirstOrDefaultAsync(p => p.Id == id && (isAdmin || p.UserId == user.Id));

            if (permit == null) return NotFound();

            if (permit.Status != PermitStatus.Approved || permit.QRCodeData == null)
            {
                TempData["Error"] = "Printable permit is only available for approved permits.";
                return RedirectToAction("Details", new { id });
            }

            return View(permit);
        }
    }
}