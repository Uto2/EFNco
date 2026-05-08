namespace EFNco.Models
{
    public class GateVerifyResultViewModel
    {
        public bool IsValid { get; set; }
        public LogAction Action { get; set; }
        public string PlateNumber { get; set; } = string.Empty;
        public int? PermitId { get; set; }
        public string? HolderName { get; set; }
        public string? Department { get; set; }
        public string? VehicleDisplay { get; set; }
        public string? PermitType { get; set; }
        public DateTime? ValidUntil { get; set; }
        public TimeSpan? ParkingDuration { get; set; }
        public int? LogId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string? InvalidReason { get; set; }

        public string DurationDisplay
        {
            get
            {
                if (ParkingDuration == null) return "—";
                var d = ParkingDuration.Value;
                if (d.TotalHours >= 1)
                    return $"{(int)d.TotalHours}h {d.Minutes}m";
                if (d.TotalMinutes >= 1)
                    return $"{d.Minutes}m {d.Seconds}s";
                return $"{d.Seconds}s";
            }
        }
    }
}
