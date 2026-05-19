using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace EFNco.Models
{
    public class AuthorizedPersonViewModel
    {
        public int Id { get; set; }

        [Required]
        public int PermitId { get; set; }

        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string? FullName { get; set; }

        [Required(ErrorMessage = "ID number is required.")]
        [StringLength(50)]
        [Display(Name = "ID Number")]
        public string? IdNumber { get; set; }

        [Required(ErrorMessage = "Relationship is required.")]
        [StringLength(50)]
        [Display(Name = "Relationship to Owner")]
        public string? Relationship { get; set; }

        [StringLength(20)]
        [Display(Name = "Contact Number")]
        public string? ContactNumber { get; set; }

        [EmailAddress]
        [StringLength(100)]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Display(Name = "Photo (JPG/PNG, max 5MB)")]
        public IFormFile? PhotoFile { get; set; }

        public bool HasPhoto { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class AuthorizedPersonSummary
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Relationship { get; set; } = string.Empty;
        public string IdNumber { get; set; } = string.Empty;
        public bool HasPhoto { get; set; }
    }

    public class ParkingSessionViewModel
    {
        public string PlateNumber { get; set; } = string.Empty;
        public DateTime EntryTime { get; set; }
        public DateTime? ExitTime { get; set; }
        public TimeSpan? Duration { get; set; }
        public string DurationDisplay { get; set; } = "Still Inside";

        public string EntryDisplay => EntryTime.ToString("MMM dd, yyyy h:mm tt");
        public string ExitDisplay => ExitTime?.ToString("MMM dd, yyyy h:mm tt") ?? "—";
    }

    public class DurationSettingViewModel
    {
        public int Id { get; set; }
        public PermitType PermitType { get; set; }

        [Required]
        [Range(0.5, 24.0, ErrorMessage = "Max hours must be between 0.5 and 24.")]
        [Display(Name = "Max Parking Hours")]
        public double MaxHours { get; set; }

        [Range(0, 60, ErrorMessage = "Grace period must be between 0 and 60 minutes.")]
        [Display(Name = "Grace Period (minutes)")]
        public int GraceMinutes { get; set; }

        [Display(Name = "Auto-Issue Violation on Overtime")]
        public bool AutoViolation { get; set; }
    }

    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        [Display(Name = "Email Address")]
        public string? Email { get; set; }
    }

    public class ResetPasswordViewModel
    {
        [Required]
        public string? Token { get; set; }

        [Required]
        [EmailAddress]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string? Password { get; set; }

        [Required(ErrorMessage = "Please confirm your password.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm Password")]
        public string? ConfirmPassword { get; set; }
    }
}