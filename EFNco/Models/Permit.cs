using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EFNco.Models
{
    // ── Enums ────────────────────────────────────────────────
    public enum VehicleType
    {
        Car,
        Motorcycle
    }

    public enum PermitType
    {
        Student,
        Faculty,
        Staff
    }

    public enum PermitStatus
    {
        Pending,
        Approved,
        Rejected,
        Revoked,
        Expired
    }

    // ── Vehicle ──────────────────────────────────────────────
    public class Vehicle
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Plate number is required.")]
        [StringLength(20)]
        [Display(Name = "Plate Number")]
        public string PlateNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vehicle type is required.")]
        [Display(Name = "Vehicle Type")]
        public VehicleType VehicleType { get; set; }

        [Required(ErrorMessage = "Make is required.")]
        [StringLength(50)]
        [Display(Name = "Make (Brand)")]
        public string Make { get; set; } = string.Empty;

        [Required(ErrorMessage = "Model is required.")]
        [StringLength(50)]
        public string Model { get; set; } = string.Empty;

        [StringLength(4)]
        public string? Year { get; set; }

        [StringLength(30)]
        public string? Color { get; set; }

        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser? Owner { get; set; }

        public virtual ParkingPermit? Permit { get; set; }
    }

    // ── Parking Permit ───────────────────────────────────────
    public class ParkingPermit
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Permit Type")]
        public PermitType PermitType { get; set; }

        public PermitStatus Status { get; set; } = PermitStatus.Pending;

        [Display(Name = "Applied On")]
        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Approved / Rejected On")]
        public DateTime? ReviewedAt { get; set; }

        [Display(Name = "Valid From")]
        public DateTime? ValidFrom { get; set; }

        [Display(Name = "Valid Until")]
        public DateTime? ValidUntil { get; set; }

        [StringLength(500)]
        [Display(Name = "Remarks")]
        public string? Remarks { get; set; }

        [StringLength(500)]
        [Display(Name = "Purpose / Notes")]
        public string? Purpose { get; set; }

        // ── License Photo ─────────────────────────────────────
        public byte[]? LicensePhotoData { get; set; }

        [StringLength(255)]
        public string? LicensePhotoFileName { get; set; }

        [StringLength(100)]
        public string? LicensePhotoContentType { get; set; }

        // ── Vehicle Registration (OR/CR) ──────────────────────
        public byte[]? RegistrationFileData { get; set; }

        [StringLength(255)]
        public string? RegistrationFileName { get; set; }

        [StringLength(100)]
        public string? RegistrationFileContentType { get; set; }

        // ── QR Code ──────────────────────────────────────────
        public byte[]? QRCodeData { get; set; }

        [StringLength(64)]
        public string? QRToken { get; set; }

        // Reviewed by
        public string? ReviewedByUserId { get; set; }

        [ForeignKey("ReviewedByUserId")]
        public virtual ApplicationUser? ReviewedBy { get; set; }

        // Vehicle FK
        [Required]
        public int VehicleId { get; set; }

        [ForeignKey("VehicleId")]
        public virtual Vehicle? Vehicle { get; set; }

        // Applicant FK
        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser? Applicant { get; set; }

        // ✅ Sprint 7 — Authorized persons on this permit
        public virtual ICollection<AuthorizedPerson> AuthorizedPersons { get; set; }
            = new List<AuthorizedPerson>();

        // Computed
        public bool IsExpired => ValidUntil.HasValue && ValidUntil.Value < DateTime.UtcNow;
    }
}
