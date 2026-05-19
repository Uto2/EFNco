using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EFNco.Data;
using EFNco.Models;

namespace EFNco.Controllers
{
    [Authorize]
    public class AuthorizedPersonController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private const long MaxPhotoSize = 5 * 1024 * 1024; // 5MB
        private static readonly string[] AllowedPhotoTypes = { ".jpg", ".jpeg", ".png" };

        public AuthorizedPersonController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // ── GET: /AuthorizedPerson/Index/{permitId} ───────────
        // List all authorized persons for a permit
        public async Task<IActionResult> Index(int permitId)
        {
            var user     = await _userManager.GetUserAsync(User);
            var isAdmin  = User.IsInRole("Admin");

            var permit = await _db.ParkingPermits
                .Include(p => p.AuthorizedPersons)
                .Include(p => p.Vehicle)
                .FirstOrDefaultAsync(p => p.Id == permitId &&
                                    (isAdmin || p.UserId == user!.Id));

            if (permit == null) return NotFound();

            ViewBag.Permit = permit;
            return View(permit.AuthorizedPersons.OrderBy(a => a.FullName).ToList());
        }

        // ── GET: /AuthorizedPerson/Add/{permitId} ─────────────
        public async Task<IActionResult> Add(int permitId)
        {
            var user    = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");

            var permit = await _db.ParkingPermits
                .FirstOrDefaultAsync(p => p.Id == permitId &&
                                    (isAdmin || p.UserId == user!.Id) &&
                                     p.Status == PermitStatus.Approved);

            if (permit == null)
            {
                TempData["Error"] = "Permit not found or not yet approved.";
                return RedirectToAction("MyPermits", "Permit");
            }

            ViewBag.PermitId = permitId;
            return View(new AuthorizedPersonViewModel { PermitId = permitId });
        }

        // ── POST: /AuthorizedPerson/Add ───────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(AuthorizedPersonViewModel model)
        {
            ModelState.Remove("PhotoFile");

            if (!ModelState.IsValid)
            {
                ViewBag.PermitId = model.PermitId;
                return View(model);
            }

            var user    = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");

            var permit = await _db.ParkingPermits
                .Include(p => p.AuthorizedPersons)
                .FirstOrDefaultAsync(p => p.Id == model.PermitId &&
                                    (isAdmin || p.UserId == user!.Id));

            if (permit == null) return NotFound();

            // Handle photo upload
            byte[]? photoData        = null;
            string? photoContentType = null;
            string? photoFileName    = null;

            if (model.PhotoFile != null && model.PhotoFile.Length > 0)
            {
                if (model.PhotoFile.Length > MaxPhotoSize)
                {
                    ModelState.AddModelError("PhotoFile", "Photo must be under 5 MB.");
                    ViewBag.PermitId = model.PermitId;
                    return View(model);
                }

                var ext = Path.GetExtension(model.PhotoFile.FileName).ToLowerInvariant();
                if (!AllowedPhotoTypes.Contains(ext))
                {
                    ModelState.AddModelError("PhotoFile", "Only JPG and PNG photos are accepted.");
                    ViewBag.PermitId = model.PermitId;
                    return View(model);
                }

                using var ms = new MemoryStream();
                await model.PhotoFile.CopyToAsync(ms);
                photoData        = ms.ToArray();
                photoContentType = model.PhotoFile.ContentType;
                photoFileName    = model.PhotoFile.FileName;
            }

            var person = new AuthorizedPerson
            {
                FullName         = model.FullName!.Trim(),
                IdNumber         = model.IdNumber!.Trim(),
                Relationship     = model.Relationship!.Trim(),
                ContactNumber    = model.ContactNumber?.Trim(),
                Email            = model.Email?.Trim(),
                PhotoData        = photoData,
                PhotoContentType = photoContentType,
                PhotoFileName    = photoFileName,
                IsActive         = true,
                AddedAt          = DateTime.Now,
                PermitId         = model.PermitId,
                AddedByUserId    = user!.Id
            };

            _db.AuthorizedPersons.Add(person);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"{person.FullName} has been added as an authorized person.";
            return RedirectToAction("Index", new { permitId = model.PermitId });
        }

        // ── GET: /AuthorizedPerson/Edit/{id} ──────────────────
        public async Task<IActionResult> Edit(int id)
        {
            var user    = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");

            var person = await _db.AuthorizedPersons
                .Include(a => a.Permit)
                .FirstOrDefaultAsync(a => a.Id == id &&
                                    (isAdmin || a.Permit!.UserId == user!.Id));

            if (person == null) return NotFound();

            var model = new AuthorizedPersonViewModel
            {
                Id            = person.Id,
                PermitId      = person.PermitId,
                FullName      = person.FullName,
                IdNumber      = person.IdNumber,
                Relationship  = person.Relationship,
                ContactNumber = person.ContactNumber,
                Email         = person.Email,
                HasPhoto      = person.PhotoData != null,
                IsActive      = person.IsActive
            };

            return View(model);
        }

        // ── POST: /AuthorizedPerson/Edit ──────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AuthorizedPersonViewModel model)
        {
            ModelState.Remove("PhotoFile");

            if (!ModelState.IsValid)
                return View(model);

            var user    = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");

            var person = await _db.AuthorizedPersons
                .Include(a => a.Permit)
                .FirstOrDefaultAsync(a => a.Id == model.Id &&
                                    (isAdmin || a.Permit!.UserId == user!.Id));

            if (person == null) return NotFound();

            person.FullName      = model.FullName!.Trim();
            person.IdNumber      = model.IdNumber!.Trim();
            person.Relationship  = model.Relationship!.Trim();
            person.ContactNumber = model.ContactNumber?.Trim();
            person.Email         = model.Email?.Trim();
            person.IsActive      = model.IsActive;

            // Update photo only if a new one is uploaded
            if (model.PhotoFile != null && model.PhotoFile.Length > 0)
            {
                var ext = Path.GetExtension(model.PhotoFile.FileName).ToLowerInvariant();
                if (!AllowedPhotoTypes.Contains(ext))
                {
                    ModelState.AddModelError("PhotoFile", "Only JPG and PNG photos are accepted.");
                    return View(model);
                }

                using var ms = new MemoryStream();
                await model.PhotoFile.CopyToAsync(ms);
                person.PhotoData        = ms.ToArray();
                person.PhotoContentType = model.PhotoFile.ContentType;
                person.PhotoFileName    = model.PhotoFile.FileName;
            }

            await _db.SaveChangesAsync();

            TempData["Success"] = $"{person.FullName} has been updated.";
            return RedirectToAction("Index", new { permitId = person.PermitId });
        }

        // ── GET: /AuthorizedPerson/Photo/{id} ─────────────────
        // Streams the photo for display
        public async Task<IActionResult> Photo(int id)
        {
            var user    = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin") || User.IsInRole("Guard");

            var person = await _db.AuthorizedPersons
                .Include(a => a.Permit)
                .FirstOrDefaultAsync(a => a.Id == id &&
                                    (isAdmin || a.Permit!.UserId == user!.Id));

            if (person == null || person.PhotoData == null) return NotFound();

            return File(person.PhotoData,
                        person.PhotoContentType ?? "image/jpeg",
                        person.PhotoFileName ?? "photo.jpg");
        }

        // ── POST: /AuthorizedPerson/Deactivate/{id} ───────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int id)
        {
            var user    = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");

            var person = await _db.AuthorizedPersons
                .Include(a => a.Permit)
                .FirstOrDefaultAsync(a => a.Id == id &&
                                    (isAdmin || a.Permit!.UserId == user!.Id));

            if (person == null) return NotFound();

            person.IsActive = false;
            await _db.SaveChangesAsync();

            TempData["Success"] = $"{person.FullName} has been deactivated.";
            return RedirectToAction("Index", new { permitId = person.PermitId });
        }

        // ── POST: /AuthorizedPerson/Delete/{id} ───────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user    = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");

            var person = await _db.AuthorizedPersons
                .Include(a => a.Permit)
                .FirstOrDefaultAsync(a => a.Id == id &&
                                    (isAdmin || a.Permit!.UserId == user!.Id));

            if (person == null) return NotFound();

            var permitId = person.PermitId;
            _db.AuthorizedPersons.Remove(person);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"{person.FullName} has been removed.";
            return RedirectToAction("Index", new { permitId });
        }
    }
}
