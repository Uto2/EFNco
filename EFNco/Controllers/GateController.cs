using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EFNco.Data;
using EFNco.Models;
using EFNco.Services;

namespace EFNco.Controllers
{
    [Authorize(Roles = "Admin,Guard")]
    public class GateController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;

        public GateController(ApplicationDbContext db,
                              UserManager<ApplicationUser> userManager,
                              IEmailService emailService)
        {
            _db          = db;
            _userManager = userManager;
            _emailService = emailService;
        }

        // ── GET: /Gate/Scan ───────────────────────────────────
        public IActionResult Scan() => View();

        // ── POST: /Gate/Verify ────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Verify(string? qrToken, string? plateNumber)
        {
            var guard = await _userManager.GetUserAsync(User);
            if (guard == null) return RedirectToAction("Login", "Account");

            ParkingPermit? permit = null;

            // ── QR Token lookup ───────────────────────────────
            if (!string.IsNullOrWhiteSpace(qrToken))
            {
                permit = await _db.ParkingPermits
                    .Include(p => p.Vehicle)
                    .Include(p => p.Applicant)
                    .Include(p => p.AuthorizedPersons)
                    .FirstOrDefaultAsync(p => p.QRToken == qrToken.Trim());
            }
            // ── Plate number fallback ─────────────────────────
            else if (!string.IsNullOrWhiteSpace(plateNumber))
            {
                var plate = plateNumber.ToUpper().Trim();
                permit = await _db.ParkingPermits
                    .Include(p => p.Vehicle)
                    .Include(p => p.Applicant)
                    .Include(p => p.AuthorizedPersons)
                    .Where(p => p.Vehicle!.PlateNumber == plate &&
                                p.Status == PermitStatus.Approved)
                    .OrderByDescending(p => p.AppliedAt)
                    .FirstOrDefaultAsync();
            }

            // ── Not found ─────────────────────────────────────
            if (permit == null)
            {
                return View("VerifyResult", new GateVerifyResultViewModel
                {
                    IsValid       = false,
                    PlateNumber   = plateNumber?.ToUpper().Trim() ?? "Unknown",
                    InvalidReason = "No active permit found for this vehicle."
                });
            }

            // ── Status check ──────────────────────────────────
            if (permit.Status != PermitStatus.Approved)
            {
                return View("VerifyResult", new GateVerifyResultViewModel
                {
                    IsValid       = false,
                    PlateNumber   = permit.Vehicle!.PlateNumber,
                    PermitId      = permit.Id,
                    HolderName    = permit.Applicant!.FullName,
                    InvalidReason = $"Permit is {permit.Status}."
                });
            }

            if (permit.IsExpired)
            {
                return View("VerifyResult", new GateVerifyResultViewModel
                {
                    IsValid       = false,
                    PlateNumber   = permit.Vehicle!.PlateNumber,
                    PermitId      = permit.Id,
                    HolderName    = permit.Applicant!.FullName,
                    InvalidReason = $"Permit expired on {permit.ValidUntil?.ToString("MMM dd, yyyy")}."
                });
            }

            // ── Entry/Exit logic ──────────────────────────────
            var lastLog = await _db.EntryExitLogs
                .Where(l => l.PermitId == permit.Id)
                .OrderByDescending(l => l.Timestamp)
                .FirstOrDefaultAsync();

            bool isCurrentlyInside = lastLog?.Action == LogAction.Entry;
            var  action            = isCurrentlyInside ? LogAction.Exit : LogAction.Entry;

            // ── Duration calculation ──────────────────────────
            TimeSpan? duration = null;
            bool isOvertime    = false;
            double overtimeMinutes = 0;

            if (action == LogAction.Exit && lastLog != null)
            {
                duration = DateTime.Now - lastLog.Timestamp;

                // ── Duration limit enforcement (7.5) ─────────
                var setting = await _db.ParkingDurationSettings
                    .FirstOrDefaultAsync(s => s.PermitType == permit.PermitType);

                if (setting != null && duration.HasValue)
                {
                    var maxDuration    = TimeSpan.FromHours(setting.MaxHours);
                    var graceLimit     = maxDuration + TimeSpan.FromMinutes(setting.GraceMinutes);

                    if (duration.Value > graceLimit)
                    {
                        isOvertime     = true;
                        overtimeMinutes = (duration.Value - maxDuration).TotalMinutes;

                        // ── Auto-violation (7.6) ──────────────
                        if (setting.AutoViolation && permit.Applicant != null)
                        {
                            var violation = new Violation
                            {
                                ViolationType  = ViolationType.Overstay,
                                FineAmount     = Violation.GetPresetFine(ViolationType.Overstay),
                                PlateNumber    = permit.Vehicle!.PlateNumber,
                                Notes          = $"Auto-issued: overtime by {(int)overtimeMinutes}m. Max allowed: {setting.MaxHours}h.",
                                UserId         = permit.UserId,
                                IssuedByUserId = guard.Id,
                                PermitId       = permit.Id,
                                IssuedAt       = DateTime.Now
                            };

                            _db.Violations.Add(violation);

                            // In-app notification
                            _db.AppNotifications.Add(new AppNotification
                            {
                                UserId    = permit.UserId,
                                Message   = $"Overstay violation auto-issued for {permit.Vehicle!.PlateNumber}. Fine: ₱{violation.FineAmount:N2}",
                                Link      = $"/Violation/Details/{violation.Id}",
                                CreatedAt = DateTime.Now
                            });
                        }

                        // ── Overtime email alert (7.6) ────────
                        if (permit.Applicant?.Email != null)
                        {
                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    await _emailService.SendOvertimeAlertAsync(
                                        permit.Applicant.Email,
                                        permit.Applicant.FullName,
                                        permit.Vehicle!.PlateNumber,
                                        (int)overtimeMinutes,
                                        setting.MaxHours);
                                }
                                catch { /* non-critical */ }
                            });
                        }
                    }
                }
            }

            // ── Log the entry/exit ────────────────────────────
            var log = new EntryExitLog
            {
                Action           = action,
                Timestamp        = DateTime.Now,
                ParkingDuration  = duration,
                PlateNumber      = permit.Vehicle!.PlateNumber,
                PermitId         = permit.Id,
                VerifiedByUserId = guard.Id
            };

            _db.EntryExitLogs.Add(log);
            await _db.SaveChangesAsync();

            // ── Build authorized persons list for result view ─
            var authorizedPersons = permit.AuthorizedPersons
                .Where(a => a.IsActive)
                .Select(a => new AuthorizedPersonSummary
                {
                    Id           = a.Id,
                    FullName     = a.FullName,
                    Relationship = a.Relationship,
                    IdNumber     = a.IdNumber,
                    HasPhoto     = a.PhotoData != null
                })
                .ToList();

            return View("VerifyResult", new GateVerifyResultViewModel
            {
                IsValid            = true,
                Action             = action,
                PlateNumber        = permit.Vehicle.PlateNumber,
                PermitId           = permit.Id,
                HolderName         = permit.Applicant!.FullName,
                Department         = permit.Applicant.Department,
                VehicleDisplay     = $"{permit.Vehicle.Make} {permit.Vehicle.Model} ({permit.Vehicle.VehicleType})",
                PermitType         = permit.PermitType.ToString(),
                ValidUntil         = permit.ValidUntil,
                ParkingDuration    = duration,
                LogId              = log.Id,
                Timestamp          = log.Timestamp,
                IsOvertime         = isOvertime,
                OvertimeMinutes    = overtimeMinutes,
                AuthorizedPersons  = authorizedPersons
            });
        }

        // ── GET: /Gate/Log ────────────────────────────────────
        public async Task<IActionResult> Log(string? date, string? action, int page = 1)
        {
            const int pageSize = 20;

            var query = _db.EntryExitLogs
                .Include(l => l.Permit)
                    .ThenInclude(p => p!.Vehicle)
                .Include(l => l.VerifiedBy)
                .AsQueryable();

            if (!string.IsNullOrEmpty(date) && DateTime.TryParse(date, out var filterDate))
                query = query.Where(l => l.Timestamp.Date == filterDate.Date);
            else
                query = query.Where(l => l.Timestamp.Date == DateTime.Today);

            if (!string.IsNullOrEmpty(action) && Enum.TryParse<LogAction>(action, out var filterAction))
                query = query.Where(l => l.Action == filterAction);

            var total = await query.CountAsync();
            var logs  = await query
                .OrderByDescending(l => l.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Date           = string.IsNullOrEmpty(date) ? DateTime.Today.ToString("yyyy-MM-dd") : date;
            ViewBag.Action         = action ?? "All";
            ViewBag.Page           = page;
            ViewBag.TotalPages     = (int)Math.Ceiling(total / (double)pageSize);
            ViewBag.TotalCount     = total;
            ViewBag.TodayEntries   = await _db.EntryExitLogs.CountAsync(l => l.Timestamp.Date == DateTime.Today && l.Action == LogAction.Entry);
            ViewBag.TodayExits     = await _db.EntryExitLogs.CountAsync(l => l.Timestamp.Date == DateTime.Today && l.Action == LogAction.Exit);
            ViewBag.CurrentlyInside = await GetCurrentlyInsideCountAsync();

            return View(logs);
        }

        // ── GET: /Gate/DurationHistory/{permitId} (7.7) ───────
        public async Task<IActionResult> DurationHistory(int permitId)
        {
            var user    = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin") || User.IsInRole("Guard");

            var permit = await _db.ParkingPermits
                .Include(p => p.Vehicle)
                .Include(p => p.Applicant)
                .FirstOrDefaultAsync(p => p.Id == permitId &&
                                    (isAdmin || p.UserId == user!.Id));

            if (permit == null) return NotFound();

            // Pair entry/exit logs into sessions
            var logs = await _db.EntryExitLogs
                .Where(l => l.PermitId == permitId)
                .OrderBy(l => l.Timestamp)
                .ToListAsync();

            var sessions = new List<ParkingSessionViewModel>();

            for (int i = 0; i < logs.Count; i++)
            {
                if (logs[i].Action == LogAction.Entry)
                {
                    var entry = logs[i];
                    var exit  = (i + 1 < logs.Count && logs[i + 1].Action == LogAction.Exit)
                                ? logs[i + 1] : null;

                    sessions.Add(new ParkingSessionViewModel
                    {
                        EntryTime       = entry.Timestamp,
                        ExitTime        = exit?.Timestamp,
                        Duration        = exit?.ParkingDuration,
                        DurationDisplay = exit?.DurationDisplay ?? "Still Inside",
                        PlateNumber     = entry.PlateNumber
                    });

                    if (exit != null) i++; // skip the exit log
                }
            }

            ViewBag.Permit = permit;
            return View(sessions.OrderByDescending(s => s.EntryTime).ToList());
        }

        private async Task<int> GetCurrentlyInsideCountAsync()
        {
            var lastLogs = await _db.EntryExitLogs
                .GroupBy(l => l.PermitId)
                .Select(g => g.OrderByDescending(l => l.Timestamp).First())
                .ToListAsync();

            return lastLogs.Count(l => l.Action == LogAction.Entry);
        }
    }
}
