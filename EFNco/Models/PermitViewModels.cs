using System.ComponentModel.DataAnnotations;

namespace EFNco.Models
{
    // ── Apply for Permit (combines Vehicle + Permit in one form) ──
    public class ApplyPermitViewModel
    {
        // Vehicle Info
        [Required(ErrorMessage = "Plate number is required.")]
        [StringLength(20)]
        [Display(Name = "Plate Number")]
        public string? PlateNumber { get; set; }

        [Required(ErrorMessage = "Please select a vehicle type.")]
        [Display(Name = "Vehicle Type")]
        public VehicleType? VehicleType { get; set; }

        [Required(ErrorMessage = "Make/Brand is required.")]
        [StringLength(50)]
        [Display(Name = "Make / Brand")]
        public string? Make { get; set; }

        [Required(ErrorMessage = "Model is required.")]
        [StringLength(50)]
        [Display(Name = "Model")]
        public string? Model { get; set; }

        [StringLength(4)]
        [Display(Name = "Year")]
        public string? Year { get; set; }

        [StringLength(30)]
        [Display(Name = "Color")]
        public string? Color { get; set; }

        // Permit Info
        [Required(ErrorMessage = "Please select a permit type.")]
        [Display(Name = "Permit Type")]
        public PermitType? PermitType { get; set; }

        [StringLength(500)]
        [Display(Name = "Purpose / Additional Notes")]
        public string? Purpose { get; set; }
    }

    // ── Permit List Item (user side) ──────────────────────────
    public class MyPermitViewModel
    {
        public int PermitId { get; set; }
        public int VehicleId { get; set; }
        public string? PlateNumber { get; set; }
        public string VehicleDisplay { get; set; } = string.Empty; // e.g. "Toyota Vios (Car)"
        public PermitType PermitType { get; set; }
        public PermitStatus Status { get; set; }
        public DateTime AppliedAt { get; set; }
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidUntil { get; set; }
        public string? Remarks { get; set; }
    }

    // ── Admin Permit List Item ────────────────────────────────
    public class AdminPermitListViewModel
    {
        public int PermitId { get; set; }
        public string ApplicantName { get; set; } = string.Empty;
        public string ApplicantEmail { get; set; } = string.Empty;
        public string? PlateNumber { get; set; }
        public string VehicleDisplay { get; set; } = string.Empty;
        public PermitType PermitType { get; set; }
        public PermitStatus Status { get; set; }
        public DateTime AppliedAt { get; set; }
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidUntil { get; set; }
    }

    // ── Admin Review (Approve / Reject) ───────────────────────
    public class ReviewPermitViewModel
    {
        public int PermitId { get; set; }
        public string ApplicantName { get; set; } = string.Empty;
        public string ApplicantEmail { get; set; } = string.Empty;
        public string? PlateNumber { get; set; }
        public string VehicleDisplay { get; set; } = string.Empty;
        public PermitType PermitType { get; set; }
        public PermitStatus Status { get; set; }
        public DateTime AppliedAt { get; set; }
        public string? Purpose { get; set; }

        // Admin fills these
        [Display(Name = "Valid From")]
        public DateTime? ValidFrom { get; set; }

        [Display(Name = "Valid Until")]
        public DateTime? ValidUntil { get; set; }

        [StringLength(500)]
        [Display(Name = "Remarks (optional)")]
        public string? Remarks { get; set; }
    }
}
