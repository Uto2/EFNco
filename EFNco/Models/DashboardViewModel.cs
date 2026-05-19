namespace EFNco.Models
{
    // ── Main Dashboard ViewModel ─────────────────────────────
    public class DashboardViewModel
    {
        // ── Summary Stats ─────────────────────────────────────
        public int ActivePermits     { get; set; }
        public int PendingPermits    { get; set; }
        public int RejectedPermits   { get; set; }
        public int ExpiredPermits    { get; set; }
        public int RevokedPermits    { get; set; }
        public int TotalPermits      { get; set; }
        public int TotalUsers        { get; set; }
        public int TodayEntries      { get; set; }
        public int TodayExits        { get; set; }
        public int CurrentlyInside   { get; set; }
        public int TotalLogs         { get; set; }

        // ── Approval Rate ─────────────────────────────────────
        public double ApprovalRate   { get; set; }  // percentage

        // ── Date Range Filter ─────────────────────────────────
        public DateTime DateFrom     { get; set; }
        public DateTime DateTo       { get; set; }

        // ── Charts Data ───────────────────────────────────────

        // Daily Entry/Exit — labels = dates, entries/exits = counts
        public List<string> DailyLabels    { get; set; } = new();
        public List<int>    DailyEntries   { get; set; } = new();
        public List<int>    DailyExits     { get; set; } = new();

        // Peak Hours — 0-23 hours
        public List<int>    HourlyEntries  { get; set; } = new();

        // Permit Status Breakdown — for donut chart
        public List<string> StatusLabels   { get; set; } = new();
        public List<int>    StatusCounts   { get; set; } = new();

        // User Growth — daily new registrations
        public List<string> UserGrowthLabels { get; set; } = new();
        public List<int>    UserGrowthCounts { get; set; } = new();

        // Permit Type Breakdown
        public List<string> PermitTypeLabels { get; set; } = new();
        public List<int>    PermitTypeCounts { get; set; } = new();

        // ── Recent Activity ───────────────────────────────────
        public List<RecentLogItem>    RecentLogs    { get; set; } = new();
        public List<RecentPermitItem> RecentPermits { get; set; } = new();
    }

    public class RecentLogItem
    {
        public string PlateNumber  { get; set; } = string.Empty;
        public string HolderName   { get; set; } = string.Empty;
        public string Action       { get; set; } = string.Empty;
        public DateTime Timestamp  { get; set; }
        public string? Duration    { get; set; }
    }

    public class RecentPermitItem
    {
        public int    PermitId      { get; set; }
        public string ApplicantName { get; set; } = string.Empty;
        public string PlateNumber   { get; set; } = string.Empty;
        public string PermitType    { get; set; } = string.Empty;
        public string Status        { get; set; } = string.Empty;
        public DateTime AppliedAt   { get; set; }
    }
}
