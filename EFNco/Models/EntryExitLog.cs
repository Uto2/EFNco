using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EFNco.Models
{
    public enum LogAction
    {
        Entry,
        Exit
    }

    public class EntryExitLog
    {
        [Key]
        public int Id { get; set; }

        // Action type
        public LogAction Action { get; set; }

        // Timestamps
        public DateTime Timestamp { get; set; } = DateTime.Now;

        // Duration — filled on Exit
        public TimeSpan? ParkingDuration { get; set; }

        // Vehicle info (denormalized for history even if permit deleted)
        [StringLength(20)]
        public string PlateNumber { get; set; } = string.Empty;

        // Permit FK
        public int? PermitId { get; set; }

        [ForeignKey("PermitId")]
        public virtual ParkingPermit? Permit { get; set; }

        // Guard who verified
        [Required]
        public string VerifiedByUserId { get; set; } = string.Empty;

        [ForeignKey("VerifiedByUserId")]
        public virtual ApplicationUser? VerifiedBy { get; set; }

        // Notes (optional, guard can add remarks)
        [StringLength(200)]
        public string? Notes { get; set; }

        // Computed helper
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
