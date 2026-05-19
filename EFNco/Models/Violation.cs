using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EFNco.Models
{
    // ── Enums ─────────────────────────────────────────────────
    public enum ViolationType
    {
        Overstay,
        NoPermit,
        ExpiredPermit,
        UnauthorizedVehicle,
        WrongParkingZone
    }

    public enum ViolationStatus
    {
        Unpaid,
        Paid,
        Appealed,
        Resolved,
        Dismissed
    }

    // ── Violation ─────────────────────────────────────────────
    public class Violation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public ViolationType ViolationType { get; set; }

        public ViolationStatus Status { get; set; } = ViolationStatus.Unpaid;

        // Fine amount — preset per type
        [Column(TypeName = "decimal(10,2)")]
        public decimal FineAmount { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public DateTime IssuedAt { get; set; } = DateTime.Now;
        public DateTime? ResolvedAt { get; set; }

        // Vehicle info (denormalized)
        [Required]
        [StringLength(20)]
        public string PlateNumber { get; set; } = string.Empty;

        // Permit FK (nullable — violation can exist without a permit)
        public int? PermitId { get; set; }

        [ForeignKey("PermitId")]
        public virtual ParkingPermit? Permit { get; set; }

        // Violator FK
        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        // Issued by (Guard or Admin)
        [Required]
        public string IssuedByUserId { get; set; } = string.Empty;

        [ForeignKey("IssuedByUserId")]
        public virtual ApplicationUser? IssuedBy { get; set; }

        // Navigation
        public virtual ViolationAppeal? Appeal { get; set; }

        // Computed helpers
        public string ViolationTypeDisplay => ViolationType switch
        {
            ViolationType.Overstay           => "Overstay",
            ViolationType.NoPermit           => "No Permit",
            ViolationType.ExpiredPermit      => "Expired Permit",
            ViolationType.UnauthorizedVehicle => "Unauthorized Vehicle",
            ViolationType.WrongParkingZone   => "Wrong Parking Zone",
            _                                => ViolationType.ToString()
        };

        // Preset fine amounts
        public static decimal GetPresetFine(ViolationType type) => type switch
        {
            ViolationType.Overstay           => 500m,
            ViolationType.NoPermit           => 1000m,
            ViolationType.ExpiredPermit      => 750m,
            ViolationType.UnauthorizedVehicle => 1500m,
            ViolationType.WrongParkingZone   => 500m,
            _                                => 500m
        };
    }

    // ── Violation Appeal ──────────────────────────────────────
    public class ViolationAppeal
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(1000)]
        public string Reason { get; set; } = string.Empty;

        public DateTime SubmittedAt { get; set; } = DateTime.Now;
        public DateTime? ReviewedAt { get; set; }

        [StringLength(500)]
        public string? AdminResponse { get; set; }

        public bool IsApproved { get; set; } = false;
        public bool IsReviewed { get; set; } = false;

        // Violation FK
        [Required]
        public int ViolationId { get; set; }

        [ForeignKey("ViolationId")]
        public virtual Violation? Violation { get; set; }

        // Reviewed by
        public string? ReviewedByUserId { get; set; }

        [ForeignKey("ReviewedByUserId")]
        public virtual ApplicationUser? ReviewedBy { get; set; }
    }

    // ── In-App Notification ───────────────────────────────────
    public class AppNotification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Message { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Link { get; set; }

        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }
    }
}
