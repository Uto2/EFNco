using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rotativa.AspNetCore;
using EFNco.Data;
using EFNco.Models;

namespace EFNco.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // ── GET: /Dashboard/Index ────────────────────────────
        public async Task<IActionResult> Index(DateTime? from, DateTime? to)
        {
            var dateFrom = from?.Date ?? DateTime.Today.AddDays(-29);
            var dateTo   = to?.Date   ?? DateTime.Today;

            // Clamp to valid range
            if (dateFrom > dateTo) dateFrom = dateTo.AddDays(-29);

            var model = await BuildDashboardAsync(dateFrom, dateTo);
            return View(model);
        }

        // ── GET: /Dashboard/ExportPdf ─────────────────────────
        public async Task<IActionResult> ExportPdf(DateTime? from, DateTime? to)
        {
            var dateFrom = from?.Date ?? DateTime.Today.AddDays(-29);
            var dateTo   = to?.Date   ?? DateTime.Today;

            var model = await BuildDashboardAsync(dateFrom, dateTo);

            return new ViewAsPdf("DashboardPdf", model)
            {
                FileName    = $"EFNco-Report-{dateFrom:yyyy-MM-dd}-to-{dateTo:yyyy-MM-dd}.pdf",
                PageSize    = Rotativa.AspNetCore.Options.Size.A4,
                PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait,
                PageMargins = new Rotativa.AspNetCore.Options.Margins(15, 15, 15, 15)
            };
        }

        // ── Shared builder ────────────────────────────────────
        private async Task<DashboardViewModel> BuildDashboardAsync(DateTime dateFrom, DateTime dateTo)
        {
            var dateToCeil = dateTo.AddDays(1); // inclusive end

            // ── Summary Stats ─────────────────────────────────
            var permitCounts = await _db.ParkingPermits
                .GroupBy(p => p.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            int active   = permitCounts.FirstOrDefault(x => x.Status == PermitStatus.Approved)?.Count  ?? 0;
            int pending  = permitCounts.FirstOrDefault(x => x.Status == PermitStatus.Pending)?.Count   ?? 0;
            int rejected = permitCounts.FirstOrDefault(x => x.Status == PermitStatus.Rejected)?.Count  ?? 0;
            int expired  = permitCounts.FirstOrDefault(x => x.Status == PermitStatus.Expired)?.Count   ?? 0;
            int revoked  = permitCounts.FirstOrDefault(x => x.Status == PermitStatus.Revoked)?.Count   ?? 0;
            int total    = permitCounts.Sum(x => x.Count);

            int reviewed = active + rejected + revoked;
            double approvalRate = reviewed > 0 ? Math.Round((double)active / reviewed * 100, 1) : 0;

            int totalUsers = await _userManager.Users.CountAsync();

            // ── Today stats ───────────────────────────────────
            int todayEntries = await _db.EntryExitLogs
                .CountAsync(l => l.Timestamp.Date == DateTime.Today && l.Action == LogAction.Entry);
            int todayExits   = await _db.EntryExitLogs
                .CountAsync(l => l.Timestamp.Date == DateTime.Today && l.Action == LogAction.Exit);

            var lastLogs = await _db.EntryExitLogs
                .GroupBy(l => l.PermitId)
                .Select(g => g.OrderByDescending(l => l.Timestamp).First())
                .ToListAsync();
            int currentlyInside = lastLogs.Count(l => l.Action == LogAction.Entry);

            int totalLogs = await _db.EntryExitLogs
                .CountAsync(l => l.Timestamp >= dateFrom && l.Timestamp < dateToCeil);

            // ── Daily Entry/Exit Chart ────────────────────────
            var logsInRange = await _db.EntryExitLogs
                .Where(l => l.Timestamp >= dateFrom && l.Timestamp < dateToCeil)
                .ToListAsync();

            var dailyLabels  = new List<string>();
            var dailyEntries = new List<int>();
            var dailyExits   = new List<int>();

            for (var d = dateFrom; d <= dateTo; d = d.AddDays(1))
            {
                dailyLabels.Add(d.ToString("MMM dd"));
                dailyEntries.Add(logsInRange.Count(l => l.Timestamp.Date == d && l.Action == LogAction.Entry));
                dailyExits.Add(logsInRange.Count(l => l.Timestamp.Date == d && l.Action == LogAction.Exit));
            }

            // ── Peak Hours Chart (today's data) ───────────────
            var todayLogs = await _db.EntryExitLogs
                .Where(l => l.Timestamp.Date == DateTime.Today && l.Action == LogAction.Entry)
                .ToListAsync();

            var hourlyEntries = Enumerable.Range(0, 24)
                .Select(h => todayLogs.Count(l => l.Timestamp.Hour == h))
                .ToList();

            // ── Permit Status Donut ───────────────────────────
            var statusLabels = new List<string> { "Active", "Pending", "Rejected", "Expired", "Revoked" };
            var statusCounts = new List<int>    { active,   pending,   rejected,   expired,   revoked  };

            // ── Permit Type Breakdown ─────────────────────────
            var typeGroups = await _db.ParkingPermits
                .GroupBy(p => p.PermitType)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToListAsync();

            var permitTypeLabels = typeGroups.Select(x => x.Type.ToString()).ToList();
            var permitTypeCounts = typeGroups.Select(x => x.Count).ToList();

            // ── User Growth (last 30 days) ────────────────────
            var usersInRange = await _userManager.Users
                .Where(u => u.CreatedAt >= dateFrom && u.CreatedAt < dateToCeil)
                .ToListAsync();

            var userGrowthLabels = new List<string>();
            var userGrowthCounts = new List<int>();

            for (var d = dateFrom; d <= dateTo; d = d.AddDays(1))
            {
                userGrowthLabels.Add(d.ToString("MMM dd"));
                userGrowthCounts.Add(usersInRange.Count(u => u.CreatedAt.Date == d));
            }

            // ── Recent Logs (last 10) ─────────────────────────
            var recentLogs = await _db.EntryExitLogs
                .Include(l => l.Permit)
                    .ThenInclude(p => p!.Applicant)
                .OrderByDescending(l => l.Timestamp)
                .Take(10)
                .Select(l => new RecentLogItem
                {
                    PlateNumber = l.PlateNumber,
                    HolderName  = l.Permit != null && l.Permit.Applicant != null
                                  ? l.Permit.Applicant.FirstName + " " + l.Permit.Applicant.LastName
                                  : "—",
                    Action      = l.Action.ToString(),
                    Timestamp   = l.Timestamp,
                    Duration    = l.ParkingDuration.HasValue ? l.DurationDisplay : null
                })
                .ToListAsync();

            // ── Recent Permits (last 10) ──────────────────────
            var recentPermits = await _db.ParkingPermits
                .Include(p => p.Vehicle)
                .Include(p => p.Applicant)
                .OrderByDescending(p => p.AppliedAt)
                .Take(10)
                .Select(p => new RecentPermitItem
                {
                    PermitId      = p.Id,
                    ApplicantName = p.Applicant!.FirstName + " " + p.Applicant.LastName,
                    PlateNumber   = p.Vehicle!.PlateNumber,
                    PermitType    = p.PermitType.ToString(),
                    Status        = p.Status.ToString(),
                    AppliedAt     = p.AppliedAt
                })
                .ToListAsync();

            return new DashboardViewModel
            {
                ActivePermits    = active,
                PendingPermits   = pending,
                RejectedPermits  = rejected,
                ExpiredPermits   = expired,
                RevokedPermits   = revoked,
                TotalPermits     = total,
                TotalUsers       = totalUsers,
                TodayEntries     = todayEntries,
                TodayExits       = todayExits,
                CurrentlyInside  = currentlyInside,
                TotalLogs        = totalLogs,
                ApprovalRate     = approvalRate,
                DateFrom         = dateFrom,
                DateTo           = dateTo,
                DailyLabels      = dailyLabels,
                DailyEntries     = dailyEntries,
                DailyExits       = dailyExits,
                HourlyEntries    = hourlyEntries,
                StatusLabels     = statusLabels,
                StatusCounts     = statusCounts,
                UserGrowthLabels = userGrowthLabels,
                UserGrowthCounts = userGrowthCounts,
                PermitTypeLabels = permitTypeLabels,
                PermitTypeCounts = permitTypeCounts,
                RecentLogs       = recentLogs,
                RecentPermits    = recentPermits
            };
        }
    }
}
