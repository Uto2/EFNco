using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EFNco.Models
{
    // ── Authorized Person ─────────────────────────────────────
    // A secondary person authorized to use a permit (e.g. family member, colleague)
    public class AuthorizedPerson
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [Display(Name = "ID Number")]
        public string IdNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [Display(Name = "Relationship to Owner")]
        public string Relationship { get; set; } = string.Empty;

        [StringLength(20)]
        [Display(Name = "Contact Number")]
        public string? ContactNumber { get; set; }

        [StringLength(100)]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        // Photo stored as byte[]
        public byte[]? PhotoData { get; set; }

        [StringLength(100)]
        public string? PhotoContentType { get; set; }

        [StringLength(255)]
        public string? PhotoFileName { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime AddedAt { get; set; } = DateTime.Now;

        // Permit FK — authorized person belongs to one permit
        [Required]
        public int PermitId { get; set; }

        [ForeignKey("PermitId")]
        public virtual ParkingPermit? Permit { get; set; }

        // Added by (owner or admin)
        [Required]
        public string AddedByUserId { get; set; } = string.Empty;

        [ForeignKey("AddedByUserId")]
        public virtual ApplicationUser? AddedBy { get; set; }
    }

    // ── Parking Duration Setting ──────────────────────────────
    // Admin-configurable max parking duration per permit type
    public class ParkingDurationSetting
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public PermitType PermitType { get; set; }

        // Maximum allowed parking hours (e.g. 4.0 for 4 hours)
        [Required]
        [Range(0.5, 24.0)]
        [Display(Name = "Max Parking Hours")]
        public double MaxHours { get; set; } = 8.0;

        // Grace period in minutes before overtime alert fires
        [Range(0, 60)]
        [Display(Name = "Grace Period (minutes)")]
        public int GraceMinutes { get; set; } = 15;

        // Whether to auto-issue a violation on overtime
        [Display(Name = "Auto-Issue Violation on Overtime")]
        public bool AutoViolation { get; set; } = false;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [StringLength(450)]
        public string? UpdatedByUserId { get; set; }

        [ForeignKey("UpdatedByUserId")]
        public virtual ApplicationUser? UpdatedBy { get; set; }
    }
}
