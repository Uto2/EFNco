using System.ComponentModel.DataAnnotations;

namespace EFNco.Models
{
    // ── Log Violation (Guard/Admin) ───────────────────────────
    public class LogViolationViewModel
    {
        [Required(ErrorMessage = "Plate number is required.")]
        [StringLength(20)]
        [Display(Name = "Plate Number")]
        public string? PlateNumber { get; set; }

        [Required(ErrorMessage = "Please select a violation type.")]
        [Display(Name = "Violation Type")]
        public string? ViolationType { get; set; }

        [StringLength(500)]
        [Display(Name = "Notes")]
        public string? Notes { get; set; }

        // Pre-filled from gate verify result
        public int? PermitId { get; set; }
        public string? ViolatorUserId { get; set; }

        // Display only
        public decimal PresetFine { get; set; }
    }

    // ── Violation List Item ───────────────────────────────────
    public class ViolationListViewModel
    {
        public int Id { get; set; }
        public string PlateNumber { get; set; } = string.Empty;
        public string ViolatorName { get; set; } = string.Empty;
        public string ViolationTypeDisplay { get; set; } = string.Empty;
        public decimal FineAmount { get; set; }
        public ViolationStatus Status { get; set; }
        public DateTime IssuedAt { get; set; }
        public bool HasAppeal { get; set; }
        public string IssuedByName { get; set; } = string.Empty;
    }

    // ── Violation Details ─────────────────────────────────────
    public class ViolationDetailsViewModel
    {
        public int Id { get; set; }
        public string PlateNumber { get; set; } = string.Empty;
        public string ViolatorName { get; set; } = string.Empty;
        public string ViolatorEmail { get; set; } = string.Empty;
        public string ViolationTypeDisplay { get; set; } = string.Empty;
        public decimal FineAmount { get; set; }
        public ViolationStatus Status { get; set; }
        public string? Notes { get; set; }
        public DateTime IssuedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string IssuedByName { get; set; } = string.Empty;
        public int? PermitId { get; set; }

        // Appeal
        public bool HasAppeal { get; set; }
        public string? AppealReason { get; set; }
        public DateTime? AppealSubmittedAt { get; set; }
        public bool AppealIsReviewed { get; set; }
        public bool AppealIsApproved { get; set; }
        public string? AppealAdminResponse { get; set; }

        // Admin review fields
        [StringLength(500)]
        public string? AdminResponse { get; set; }
    }

    // ── Appeal Form ───────────────────────────────────────────
    public class AppealViewModel
    {
        public int ViolationId { get; set; }
        public string PlateNumber { get; set; } = string.Empty;
        public string ViolationTypeDisplay { get; set; } = string.Empty;
        public decimal FineAmount { get; set; }
        public DateTime IssuedAt { get; set; }
        public string? Notes { get; set; }

        [Required(ErrorMessage = "Please provide a reason for your appeal.")]
        [StringLength(1000, MinimumLength = 20, ErrorMessage = "Reason must be at least 20 characters.")]
        [Display(Name = "Reason for Appeal")]
        public string? Reason { get; set; }
    }
}
