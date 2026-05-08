using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EFNco.Data;
using EFNco.Models;

namespace EFNco.Controllers
{
    [Authorize(Roles = "Admin,Guard")]
    public class GateController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public GateController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // ── GET: /Gate/Scan ──────────────────────────────────
        // Guard's main scanning interface
        public IActionResult Scan()
        {
            return View();
        }

        // ── POST: /Gate/Verify ───────────────────────────────
        // Handles both QR token scan and manual plate/permit entry
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Verify(string? qrToken, string? plateNumber)
        {
            var guard = await _userManager.GetUserAsync(User);
            if (guard == null) return RedirectToAction("Login", "Account");

            ParkingPermit? permit = null;

            // ── Try QR Token first ────────────────────────────
            if (!string.IsNullOrWhiteSpace(qrToken))
            {
                permit = await _db.ParkingPermits
                    .Include(p => p.Vehicle)
                    .Include(p => p.Applicant)
                    .FirstOrDefaultAsync(p => p.QRToken == qrToken.Trim());
            }
            // ── Fallback: search by plate number ─────────────
            else if (!string.IsNullOrWhiteSpace(plateNumber))
            {
                var plate = plateNumber.ToUpper().Trim();
                permit = await _db.ParkingPermits
                    .Include(p => p.Vehicle)
                    .Include(p => p.Applicant)
                    .Where(p => p.Vehicle!.PlateNumber == plate &&
                           (p.Status == PermitStatus.Approved))
                    .OrderByDescending(p => p.AppliedAt)
                    .FirstOrDefaultAsync();
            }

            // ── Not found ─────────────────────────────────────
            if (permit == null)
            {
                return View("VerifyResult", new GateVerifyResultViewModel
                {
                    IsValid      = false,
                    PlateNumber  = plateNumber?.ToUpper().Trim() ?? "Unknown",
                    InvalidReason = "No active permit found for this vehicle."
                });
            }

            // ── Check permit validity ─────────────────────────
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

            // ── Check if vehicle is currently inside ──────────
            var lastLog = await _db.EntryExitLogs
                .Where(l => l.PermitId == permit.Id)
                .OrderByDescending(l => l.Timestamp)
                .FirstOrDefaultAsync();

            bool isCurrentlyInside = lastLog?.Action == LogAction.Entry;
            var action = isCurrentlyInside ? LogAction.Exit : LogAction.Entry;

            // ── Calculate duration on exit ────────────────────
            TimeSpan? duration = null;
            if (action == LogAction.Exit && lastLog != null)
                duration = DateTime.Now - lastLog.Timestamp;

            // ── Log the entry/exit ────────────────────────────
            var log = new EntryExitLog
            {
                Action             = action,
                Timestamp          = DateTime.Now,
                ParkingDuration    = duration,
                PlateNumber        = permit.Vehicle!.PlateNumber,
                PermitId           = permit.Id,
                VerifiedByUserId   = guard.Id
            };

            _db.EntryExitLogs.Add(log);
            await _db.SaveChangesAsync();

            // ── Return result ─────────────────────────────────
            return View("VerifyResult", new GateVerifyResultViewModel
            {
                IsValid          = true,
                Action           = action,
                PlateNumber      = permit.Vehicle.PlateNumber,
                PermitId         = permit.Id,
                HolderName       = permit.Applicant!.FullName,
                Department       = permit.Applicant.Department,
                VehicleDisplay   = $"{permit.Vehicle.Make} {permit.Vehicle.Model} ({permit.Vehicle.VehicleType})",
                PermitType       = permit.PermitType.ToString(),
                ValidUntil       = permit.ValidUntil,
                ParkingDuration  = duration,
                LogId            = log.Id,
                Timestamp        = log.Timestamp
            });
        }

        // ── GET: /Gate/Log ───────────────────────────────────
        // Entry/exit history — accessible to both Guard and Admin
        public async Task<IActionResult> Log(string? date, string? action, int page = 1)
        {
            const int pageSize = 20;

            var query = _db.EntryExitLogs
                .Include(l => l.Permit)
                    .ThenInclude(p => p!.Vehicle)
                .Include(l => l.VerifiedBy)
                .AsQueryable();

            // Filter by date
            if (!string.IsNullOrEmpty(date) && DateTime.TryParse(date, out var filterDate))
                query = query.Where(l => l.Timestamp.Date == filterDate.Date);
            else
                query = query.Where(l => l.Timestamp.Date == DateTime.Today);

            // Filter by action
            if (!string.IsNullOrEmpty(action) && Enum.TryParse<LogAction>(action, out var filterAction))
                query = query.Where(l => l.Action == filterAction);

            var total = await query.CountAsync();
            var logs  = await query
                .OrderByDescending(l => l.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Date        = string.IsNullOrEmpty(date) ? DateTime.Today.ToString("yyyy-MM-dd") : date;
            ViewBag.Action      = action ?? "All";
            ViewBag.Page        = page;
            ViewBag.TotalPages  = (int)Math.Ceiling(total / (double)pageSize);
            ViewBag.TotalCount  = total;

            // Stats for today
            ViewBag.TodayEntries = await _db.EntryExitLogs
                .CountAsync(l => l.Timestamp.Date == DateTime.Today && l.Action == LogAction.Entry);
            ViewBag.TodayExits   = await _db.EntryExitLogs
                .CountAsync(l => l.Timestamp.Date == DateTime.Today && l.Action == LogAction.Exit);
            ViewBag.CurrentlyInside = await GetCurrentlyInsideCountAsync();

            return View(logs);
        }

        // Helper: count vehicles currently inside
        private async Task<int> GetCurrentlyInsideCountAsync()
        {
            // Get the last log action per permit
            var lastLogs = await _db.EntryExitLogs
                .GroupBy(l => l.PermitId)
                .Select(g => g.OrderByDescending(l => l.Timestamp).First())
                .ToListAsync();

            return lastLogs.Count(l => l.Action == LogAction.Entry);
        }
    }
}
