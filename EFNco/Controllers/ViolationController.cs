using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EFNco.Data;
using EFNco.Models;
using EFNco.Services;

namespace EFNco.Controllers
{
    [Authorize]
    public class ViolationController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;

        public ViolationController(ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            IEmailService emailService)
        {
            _db = db;
            _userManager = userManager;
            _emailService = emailService;
        }

        // ── GET: /Violation/MyViolations ─────────────────────
        public async Task<IActionResult> MyViolations()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var violations = await _db.Violations
                .Include(v => v.IssuedBy)
                .Include(v => v.Appeal)
                .Where(v => v.UserId == user.Id)
                .OrderByDescending(v => v.IssuedAt)
                .Select(v => new ViolationListViewModel
                {
                    Id                   = v.Id,
                    PlateNumber          = v.PlateNumber,
                    ViolatorName         = user.FullName,
                    ViolationTypeDisplay = v.ViolationTypeDisplay,
                    FineAmount           = v.FineAmount,
                    Status               = v.Status,
                    IssuedAt             = v.IssuedAt,
                    HasAppeal            = v.Appeal != null,
                    IssuedByName         = v.IssuedBy!.FirstName + " " + v.IssuedBy.LastName
                })
                .ToListAsync();

            // ── Violation count warning for the user ──────────
            var totalViolations = violations.Count;
            var unpaidCount     = violations.Count(v => v.Status == ViolationStatus.Unpaid);
            var hasRevokedPermit = await _db.ParkingPermits
                .AnyAsync(p => p.UserId == user.Id &&
                               p.Status == PermitStatus.Revoked &&
                               (p.Remarks ?? "").Contains("exceeding the maximum"));

            // Warning levels: 0 = safe, 1 = caution (1-2), 2 = danger (3+)
            int warningLevel = totalViolations >= 3 ? 2 : totalViolations >= 1 ? 1 : 0;

            ViewBag.ViolationCount  = totalViolations;
            ViewBag.UnpaidCount     = unpaidCount;
            ViewBag.WarningLevel    = warningLevel;
            ViewBag.HasRevokedPermit = hasRevokedPermit;

            return View(violations);
        }

        // ── GET: /Violation/Details/{id} ─────────────────────
        public async Task<IActionResult> Details(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var isAdminOrGuard = User.IsInRole("Admin") || User.IsInRole("Guard");

            var v = await _db.Violations
                .Include(x => x.User)
                .Include(x => x.IssuedBy)
                .Include(x => x.Appeal)
                    .ThenInclude(a => a!.ReviewedBy)
                .FirstOrDefaultAsync(x => x.Id == id && (isAdminOrGuard || x.UserId == user.Id));

            if (v == null) return NotFound();

            var model = new ViolationDetailsViewModel
            {
                Id                   = v.Id,
                PlateNumber          = v.PlateNumber,
                ViolatorName         = v.User!.FullName,
                ViolatorEmail        = v.User.Email ?? "",
                ViolationTypeDisplay = v.ViolationTypeDisplay,
                FineAmount           = v.FineAmount,
                Status               = v.Status,
                Notes                = v.Notes,
                IssuedAt             = v.IssuedAt,
                ResolvedAt           = v.ResolvedAt,
                IssuedByName         = v.IssuedBy!.FullName,
                PermitId             = v.PermitId,
                HasAppeal            = v.Appeal != null,
                AppealReason         = v.Appeal?.Reason,
                AppealSubmittedAt    = v.Appeal?.SubmittedAt,
                AppealIsReviewed     = v.Appeal?.IsReviewed ?? false,
                AppealIsApproved     = v.Appeal?.IsApproved ?? false,
                AppealAdminResponse  = v.Appeal?.AdminResponse
            };

            return View(model);
        }

        // ── GET: /Violation/Appeal/{id} ──────────────────────
        public async Task<IActionResult> Appeal(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var v = await _db.Violations
                .Include(x => x.Appeal)
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == user.Id);

            if (v == null) return NotFound();

            if (v.Appeal != null)
            {
                TempData["Error"] = "You have already submitted an appeal for this violation.";
                return RedirectToAction("MyViolations");
            }

            if (v.Status == ViolationStatus.Paid || v.Status == ViolationStatus.Dismissed)
            {
                TempData["Error"] = "This violation cannot be appealed.";
                return RedirectToAction("MyViolations");
            }

            return View(new AppealViewModel
            {
                ViolationId          = v.Id,
                PlateNumber          = v.PlateNumber,
                ViolationTypeDisplay = v.ViolationTypeDisplay,
                FineAmount           = v.FineAmount,
                IssuedAt             = v.IssuedAt,
                Notes                = v.Notes
            });
        }

        // ── POST: /Violation/Appeal ──────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Appeal(AppealViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var v = await _db.Violations
                .Include(x => x.Appeal)
                .FirstOrDefaultAsync(x => x.Id == model.ViolationId && x.UserId == user.Id);

            if (v == null) return NotFound();
            if (v.Appeal != null)
            {
                TempData["Error"] = "Appeal already submitted.";
                return RedirectToAction("MyViolations");
            }

            var appeal = new ViolationAppeal
            {
                ViolationId  = v.Id,
                Reason       = model.Reason!,
                SubmittedAt  = DateTime.Now
            };

            v.Status = ViolationStatus.Appealed;
            _db.ViolationAppeals.Add(appeal);
            await _db.SaveChangesAsync();

            // In-app notification to Admin
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            foreach (var admin in admins)
            {
                _db.AppNotifications.Add(new AppNotification
                {
                    UserId    = admin.Id,
                    Message   = $"{user.FullName} submitted an appeal for Violation #{v.Id} ({v.ViolationTypeDisplay})",
                    Link      = $"/Violation/AdminDetails/{v.Id}",
                    CreatedAt = DateTime.Now
                });
            }
            await _db.SaveChangesAsync();

            TempData["Success"] = "Appeal submitted successfully. An administrator will review it.";
            return RedirectToAction("MyViolations");
        }

        // ── GET: /Violation/Log ──────────────────────────────
        [Authorize(Roles = "Admin,Guard")]
        public IActionResult Log(string? plateNumber = null, int? permitId = null, string? userId = null)
        {
            var model = new LogViolationViewModel
            {
                PlateNumber      = plateNumber?.ToUpper(),
                PermitId         = permitId,
                ViolatorUserId   = userId
            };
            return View(model);
        }

        // ── POST: /Violation/Log ─────────────────────────────
        [HttpPost]
        [Authorize(Roles = "Admin,Guard")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Log(LogViolationViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var issuer = await _userManager.GetUserAsync(User);
            if (issuer == null) return RedirectToAction("Login", "Account");

            if (!Enum.TryParse<ViolationType>(model.ViolationType, out var vType))
            {
                ModelState.AddModelError("ViolationType", "Invalid violation type.");
                return View(model);
            }

            // Find violator by permit or plate
            ApplicationUser? violator = null;
            int? permitId = null;

            if (!string.IsNullOrEmpty(model.ViolatorUserId))
            {
                violator = await _userManager.FindByIdAsync(model.ViolatorUserId);
            }

            if (model.PermitId.HasValue)
            {
                var permit = await _db.ParkingPermits
                    .Include(p => p.Applicant)
                    .FirstOrDefaultAsync(p => p.Id == model.PermitId.Value);
                if (permit != null)
                {
                    violator ??= permit.Applicant;
                    permitId = permit.Id;
                }
            }

            if (violator == null)
            {
                // Try to find by plate
                var vehicle = await _db.Vehicles
                    .Include(v => v.Owner)
                    .FirstOrDefaultAsync(v => v.PlateNumber == model.PlateNumber!.ToUpper().Trim());
                violator = vehicle?.Owner;
            }

            if (violator == null)
            {
                ModelState.AddModelError("PlateNumber", "No registered user found for this plate number.");
                return View(model);
            }

            var fine = Violation.GetPresetFine(vType);

            var violation = new Violation
            {
                ViolationType  = vType,
                FineAmount     = fine,
                PlateNumber    = model.PlateNumber!.ToUpper().Trim(),
                Notes          = model.Notes,
                UserId         = violator.Id,
                IssuedByUserId = issuer.Id,
                PermitId       = permitId,
                IssuedAt       = DateTime.Now
            };

            _db.Violations.Add(violation);
            await _db.SaveChangesAsync();

            // --- Third Violation Enforcement Action ---
            var violationCount = await _db.Violations.CountAsync(v => v.UserId == violator.Id);
            
            string appNotificationMsg = $"A parking violation has been issued for your vehicle {violation.PlateNumber} — {violation.ViolationTypeDisplay}. Fine: ₱{fine:N2}";
            string adminWarning = "";

            if (violationCount >= 3)
            {
                appNotificationMsg = $"🚨 FINAL WARNING: You have reached {violationCount} parking violations. Severe enforcement actions have been triggered.";
                
                // Penalty: Automatically revoke active permits
                var activePermits = await _db.ParkingPermits
                    .Where(p => p.UserId == violator.Id && p.Status == PermitStatus.Approved)
                    .ToListAsync();
                    
                foreach (var p in activePermits)
                {
                    p.Status = PermitStatus.Revoked;
                    p.Remarks = "Automatically revoked due to exceeding the maximum allowed parking violations (3).";
                }
                
                if (activePermits.Any())
                {
                    await _db.SaveChangesAsync();
                    appNotificationMsg += " Your active parking permit has been permanently REVOKED.";
                    adminWarning = " The user has 3+ violations. Their active parking permit was automatically revoked.";
                }
            }
            // ------------------------------------------

            // In-app notification to violator
            _db.AppNotifications.Add(new AppNotification
            {
                UserId    = violator.Id,
                Message   = appNotificationMsg,
                Link      = $"/Violation/Details/{violation.Id}",
                CreatedAt = DateTime.Now
            });
            await _db.SaveChangesAsync();

            // Email notification (fire and forget — don't fail if email fails)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailService.SendViolationIssuedAsync(
                        violator.Email ?? "",
                        violator.FullName,
                        violation.PlateNumber,
                        violation.ViolationTypeDisplay,
                        fine);
                }
                catch { /* Email failure is non-critical */ }
            });

            TempData["Success"] = $"Violation logged for {violation.PlateNumber}. Fine: ₱{fine:N2}. Notification sent.{adminWarning}";

            // Guards cannot access Admin/AdminViolations — redirect them to Gate/Log instead
            if (User.IsInRole("Admin"))
                return RedirectToAction("AdminViolations", "Admin");
            else
                return RedirectToAction("Log", "Gate");
        }

        // ── GET: /Violation/MarkPaid/{id} ────────────────────
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkPaid(int id)
        {
            var v = await _db.Violations.FindAsync(id);
            if (v == null) return NotFound();

            v.Status     = ViolationStatus.Paid;
            v.ResolvedAt = DateTime.Now;
            await _db.SaveChangesAsync();

            // Notify user
            _db.AppNotifications.Add(new AppNotification
            {
                UserId    = v.UserId,
                Message   = $"Your fine for Violation #{v.Id} ({v.ViolationTypeDisplay}) has been marked as paid.",
                Link      = $"/Violation/Details/{v.Id}",
                CreatedAt = DateTime.Now
            });
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Violation #{id} marked as paid.";
            return RedirectToAction("AdminViolations", "Admin");
        }
    }
}
