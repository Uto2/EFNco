using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EFNco.Data;
using EFNco.Models;

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

            // Filter by status if provided
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<PermitStatus>(status, out var permitStatus))
                query = query.Where(p => p.Status == permitStatus);

            var permits = await query
                .OrderByDescending(p => p.AppliedAt)
                .Select(p => new AdminPermitListViewModel
                {
                    PermitId       = p.Id,
                    ApplicantName  = p.Applicant!.FirstName + " " + p.Applicant.LastName,
                    ApplicantEmail = p.Applicant.Email ?? "",
                    PlateNumber    = p.Vehicle!.PlateNumber,
                    VehicleDisplay = p.Vehicle.Make + " " + p.Vehicle.Model + " (" + p.Vehicle.VehicleType + ")",
                    PermitType     = p.PermitType,
                    Status         = p.Status,
                    AppliedAt      = p.AppliedAt,
                    ValidFrom      = p.ValidFrom,
                    ValidUntil     = p.ValidUntil
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

                // ✅ These were missing — causing "No documents" to always show
                HasLicensePhoto = permit.LicensePhotoData != null,
                LicensePhotoFileName = permit.LicensePhotoFileName,
                HasRegistrationFile = permit.RegistrationFileData != null,
                RegistrationFileName = permit.RegistrationFileName,
            };

            return View(model);
        }

        // POST: Approve permit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApprovePermit(int id, DateTime validFrom, DateTime validUntil, string? remarks)
        {
            var permit = await _db.ParkingPermits.FindAsync(id);
            if (permit == null) return NotFound();

            var admin = await _userManager.GetUserAsync(User);

            permit.Status           = PermitStatus.Approved;
            permit.ReviewedAt       = DateTime.UtcNow;
            permit.ValidFrom        = validFrom;
            permit.ValidUntil       = validUntil;
            permit.Remarks          = remarks;
            permit.ReviewedByUserId = admin!.Id;

            await _db.SaveChangesAsync();

            TempData["Success"] = $"Permit #{id} approved successfully.";
            return RedirectToAction("Permits");
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
    }
}
