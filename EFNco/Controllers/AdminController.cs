using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EFNco.Data;
using EFNco.Models;
using QRCoder;

namespace EFNco.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _db;

        public AdminController(UserManager<ApplicationUser> userManager,
                               RoleManager<IdentityRole> roleManager,
                               ApplicationDbContext db)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _db = db;
        }

        // ── User Management ──────────────────────────────────

        public async Task<IActionResult> Users()
        {
            var users = _userManager.Users.ToList();
            var userList = new List<UserListViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userList.Add(new UserListViewModel
                {
                    Id         = user.Id,
                    FullName   = user.FullName,
                    Email      = user.Email ?? "",
                    Department = user.Department,
                    Role       = roles.FirstOrDefault() ?? "No Role",
                    IsActive   = user.IsActive,
                    CreatedAt  = user.CreatedAt
                });
            }

            return View(userList);
        }

        public async Task<IActionResult> UserDetails(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            var roles = await _userManager.GetRolesAsync(user);
            ViewBag.Roles = roles;
            return View(user);
        }

        public async Task<IActionResult> EditUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            var allRoles = _roleManager.Roles.Select(r => r.Name).ToList();

            var model = new EditUserViewModel
            {
                Id              = user.Id,
                FirstName       = user.FirstName,
                LastName        = user.LastName,
                Email           = user.Email ?? "",
                Department      = user.Department,
                PhoneNumber     = user.PhoneNumber,
                IsActive        = user.IsActive,
                CurrentRole     = roles.FirstOrDefault() ?? "",
                AvailableRoles  = allRoles!
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AvailableRoles = _roleManager.Roles.Select(r => r.Name!).ToList();
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound();

            user.FirstName  = model.FirstName;
            user.LastName   = model.LastName;
            user.Department = model.Department;
            user.PhoneNumber = model.PhoneNumber;
            user.IsActive   = model.IsActive;

            await _userManager.UpdateAsync(user);

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!string.IsNullOrEmpty(model.SelectedRole))
                await _userManager.AddToRoleAsync(user, model.SelectedRole);

            TempData["Success"] = $"User {user.FullName} updated successfully.";
            return RedirectToAction("Users");
        }

        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            return View(user);
        }

        [HttpPost, ActionName("DeleteUser")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUserConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.Id == user.Id)
            {
                TempData["Error"] = "You cannot delete your own account.";
                return RedirectToAction("Users");
            }

            await _userManager.DeleteAsync(user);
            TempData["Success"] = $"User {user.FullName} deleted.";
            return RedirectToAction("Users");
        }

        public async Task<IActionResult> ResetUserPassword(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            ViewBag.UserName = user.FullName;
            ViewBag.UserId   = user.Id;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetUserPassword(string id, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var token  = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (result.Succeeded)
            {
                TempData["Success"] = $"Password reset for {user.FullName}.";
                return RedirectToAction("Users");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            ViewBag.UserName = user.FullName;
            ViewBag.UserId   = user.Id;
            return View();
        }

        // ── Permit Management ────────────────────────────────

        public async Task<IActionResult> Permits(string? status)
        {
            var query = _db.ParkingPermits
                .Include(p => p.Vehicle)
                .Include(p => p.Applicant)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<PermitStatus>(status, out var permitStatus))
                query = query.Where(p => p.Status == permitStatus);

            var permits = await query
                .OrderByDescending(p => p.AppliedAt)
                .Select(p => new AdminPermitListViewModel
                {
                    PermitId = p.Id,
                    ApplicantName = p.Applicant!.FirstName + " " + p.Applicant.LastName,
                    ApplicantEmail = p.Applicant.Email ?? "",
                    PlateNumber = p.Vehicle!.PlateNumber,
                    VehicleDisplay = p.Vehicle.Make + " " + p.Vehicle.Model + " (" + p.Vehicle.VehicleType + ")",
                    PermitType = p.PermitType,
                    Status = p.Status,
                    AppliedAt = p.AppliedAt,
                    ValidFrom = p.ValidFrom,
                    ValidUntil = p.ValidUntil,

                    // ✅ These were missing — causing the Docs column to always show "—"
                    HasLicensePhoto = p.LicensePhotoData != null,
                    HasRegistrationFile = p.RegistrationFileData != null,
                })
                .ToListAsync();

            ViewBag.CurrentStatus = status ?? "All";
            return View(permits);
        }


        // ── GET: /Admin/PermitDetails/{id} ───────────────────
        // Replace ONLY this action in your existing AdminController.cs
        // Everything else in AdminController stays the same.

        public async Task<IActionResult> PermitDetails(int id)
        {
            var permit = await _db.ParkingPermits
                .Include(p => p.Vehicle)
                .Include(p => p.Applicant)
                .Include(p => p.ReviewedBy)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (permit == null) return NotFound();

            var model = new ReviewPermitViewModel
            {
                PermitId = permit.Id,
                ApplicantName = permit.Applicant!.FullName,
                ApplicantEmail = permit.Applicant.Email ?? "",
                PlateNumber = permit.Vehicle!.PlateNumber,
                VehicleDisplay = $"{permit.Vehicle.Make} {permit.Vehicle.Model} ({permit.Vehicle.VehicleType})",
                PermitType = permit.PermitType,
                Status = permit.Status,
                AppliedAt = permit.AppliedAt,
                Purpose = permit.Purpose,
                ValidFrom = permit.ValidFrom,
                ValidUntil = permit.ValidUntil,
                Remarks = permit.Remarks,
                HasQRCode = permit.QRCodeData != null,  // Sprint 3 addition

                // ✅ These were missing — causing "No documents" to always show
                HasLicensePhoto = permit.LicensePhotoData != null,
                LicensePhotoFileName = permit.LicensePhotoFileName,
                HasRegistrationFile = permit.RegistrationFileData != null,
                RegistrationFileName = permit.RegistrationFileName,
            };

            return View(model);
        }

        // Replace your ApprovePermit action in AdminController.cs with this.
        // Key fix: verifyUrl now uses Request.Host directly to guarantee
        // the correct host is encoded in the QR, not just "localhost"

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApprovePermit(int id, DateTime validFrom, DateTime validUntil, string? remarks)
        {
            var permit = await _db.ParkingPermits
                .Include(p => p.Vehicle)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (permit == null) return NotFound();

            var reviewer = await _userManager.GetUserAsync(User);

            permit.Status = PermitStatus.Approved;
            permit.ValidFrom = validFrom;
            permit.ValidUntil = validUntil;
            permit.Remarks = remarks;
            permit.ReviewedAt = DateTime.UtcNow;
            permit.ReviewedByUserId = reviewer?.Id;

            // Generate unique token
            permit.QRToken = Guid.NewGuid().ToString("N");

            // ✅ Build the verify URL using Url.Action with query string token
            // This generates: http://host/Permit/Verify?token=abc123
            var verifyUrl = Url.Action("Verify", "Permit", new { token = permit.QRToken }, Request.Scheme);

            // ✅ Debug: save the URL to TempData so you can see what's in the QR
            TempData["QRDebugUrl"] = verifyUrl;

            // Generate QR
            using var qrGenerator = new QRCodeGenerator();
            var qrData = qrGenerator.CreateQrCode(verifyUrl, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrData);
            permit.QRCodeData = qrCode.GetGraphic(10);

            await _db.SaveChangesAsync();

            TempData["Success"] = $"Permit #{id} approved. QR URL: {verifyUrl}";
            return RedirectToAction("PermitDetails", "Admin", new { id });
        }


        // POST: Reject permit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectPermit(int id, string? remarks)
        {
            var permit = await _db.ParkingPermits.FindAsync(id);
            if (permit == null) return NotFound();

            var admin = await _userManager.GetUserAsync(User);

            permit.Status           = PermitStatus.Rejected;
            permit.ReviewedAt       = DateTime.UtcNow;
            permit.Remarks          = remarks;
            permit.ReviewedByUserId = admin!.Id;

            await _db.SaveChangesAsync();

            TempData["Success"] = $"Permit #{id} rejected.";
            return RedirectToAction("Permits");
        }

        // POST: Revoke approved permit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevokePermit(int id, string? remarks)
        {
            var permit = await _db.ParkingPermits.FindAsync(id);
            if (permit == null) return NotFound();

            var admin = await _userManager.GetUserAsync(User);

            permit.Status           = PermitStatus.Revoked;
            permit.ReviewedAt       = DateTime.UtcNow;
            permit.Remarks          = remarks;
            permit.ReviewedByUserId = admin!.Id;

            await _db.SaveChangesAsync();

            TempData["Success"] = $"Permit #{id} revoked.";
            return RedirectToAction("Permits");
        }

        public async Task<IActionResult> AdminViolations(string? status)
        {
            var query = _db.Violations
                .Include(v => v.User)
                .Include(v => v.IssuedBy)
                .Include(v => v.Appeal)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<ViolationStatus>(status, out var vs))
                query = query.Where(v => v.Status == vs);

            var violations = await query
                .OrderByDescending(v => v.IssuedAt)
                .Select(v => new ViolationListViewModel
                {
                    Id = v.Id,
                    PlateNumber = v.PlateNumber,
                    ViolatorName = v.User!.FirstName + " " + v.User.LastName,
                    ViolationTypeDisplay = v.ViolationTypeDisplay,
                    FineAmount = v.FineAmount,
                    Status = v.Status,
                    IssuedAt = v.IssuedAt,
                    HasAppeal = v.Appeal != null,
                    IssuedByName = v.IssuedBy!.FirstName + " " + v.IssuedBy.LastName
                })
                .ToListAsync();

            ViewBag.CurrentStatus = status ?? "All";
            return View(violations);
        }

        // GET: /Admin/AdminViolationDetails/{id}
        public async Task<IActionResult> AdminViolationDetails(int id)
        {
            var v = await _db.Violations
                .Include(x => x.User)
                .Include(x => x.IssuedBy)
                .Include(x => x.Permit)
                .Include(x => x.Appeal)
                    .ThenInclude(a => a!.ReviewedBy)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (v == null) return NotFound();

            var model = new ViolationDetailsViewModel
            {
                Id = v.Id,
                PlateNumber = v.PlateNumber,
                ViolatorName = v.User!.FullName,
                ViolatorEmail = v.User.Email ?? "",
                ViolationTypeDisplay = v.ViolationTypeDisplay,
                FineAmount = v.FineAmount,
                Status = v.Status,
                Notes = v.Notes,
                IssuedAt = v.IssuedAt,
                ResolvedAt = v.ResolvedAt,
                IssuedByName = v.IssuedBy!.FullName,
                PermitId = v.PermitId,
                HasAppeal = v.Appeal != null,
                AppealReason = v.Appeal?.Reason,
                AppealSubmittedAt = v.Appeal?.SubmittedAt,
                AppealIsReviewed = v.Appeal?.IsReviewed ?? false,
                AppealIsApproved = v.Appeal?.IsApproved ?? false,
                AppealAdminResponse = v.Appeal?.AdminResponse
            };

            return View(model);
        }

        // POST: /Admin/ReviewAppeal/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReviewAppeal(int id, bool approve, string? response)
        {
            var appeal = await _db.ViolationAppeals
                .Include(a => a.Violation)
                .FirstOrDefaultAsync(a => a.ViolationId == id);

            if (appeal == null) return NotFound();

            var reviewer = await _userManager.GetUserAsync(User);

            appeal.IsReviewed = true;
            appeal.IsApproved = approve;
            appeal.AdminResponse = response;
            appeal.ReviewedAt = DateTime.Now;
            appeal.ReviewedByUserId = reviewer?.Id;

            // Update violation status based on appeal decision
            if (approve)
            {
                appeal.Violation!.Status = ViolationStatus.Dismissed;
                appeal.Violation.ResolvedAt = DateTime.Now;
            }
            else
            {
                appeal.Violation!.Status = ViolationStatus.Unpaid;
            }

            // Notify the violator
            _db.AppNotifications.Add(new AppNotification
            {
                UserId = appeal.Violation.UserId,
                Message = approve
                    ? $"Your appeal for Violation #{id} has been approved and the violation has been dismissed."
                    : $"Your appeal for Violation #{id} has been reviewed and denied.",
                Link = $"/Violation/Details/{id}",
                CreatedAt = DateTime.Now
            });

            await _db.SaveChangesAsync();

            TempData["Success"] = approve
                ? $"Appeal approved — Violation #{id} dismissed."
                : $"Appeal denied — Violation #{id} remains unpaid.";

            return RedirectToAction("AdminViolationDetails", new { id });
        }

        // POST: /Admin/DismissViolation/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DismissViolation(int id)
        {
            var v = await _db.Violations.FindAsync(id);
            if (v == null) return NotFound();

            v.Status = ViolationStatus.Dismissed;
            v.ResolvedAt = DateTime.Now;

            _db.AppNotifications.Add(new AppNotification
            {
                UserId = v.UserId,
                Message = $"Your Violation #{id} ({v.ViolationTypeDisplay}) has been dismissed by an administrator.",
                Link = $"/Violation/Details/{id}",
                CreatedAt = DateTime.Now
            });

            await _db.SaveChangesAsync();

            TempData["Success"] = $"Violation #{id} dismissed.";
            return RedirectToAction("AdminViolations");
        }
    }
}
