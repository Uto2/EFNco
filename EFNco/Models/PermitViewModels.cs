using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace EFNco.Models
{
    // ── Apply for Permit ──────────────────────────────────────
    public class ApplyPermitViewModel
    {
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

        // Renamed from "Model" — reserved keyword in ASP.NET Core MVC
        [Required(ErrorMessage = "Model is required.")]
        [StringLength(50)]
        [Display(Name = "Model")]
        public string? VehicleModel { get; set; }

        [StringLength(4)]
        [Display(Name = "Year")]
        public string? Year { get; set; }

        [StringLength(30)]
        [Display(Name = "Color")]
        public string? Color { get; set; }

        [Required(ErrorMessage = "Please select a permit type.")]
        [Display(Name = "Permit Type")]
        public PermitType? PermitType { get; set; }

        [StringLength(500)]
        [Display(Name = "Purpose / Additional Notes")]
        public string? Purpose { get; set; }

        [Display(Name = "Driver's License Photo")]
        public IFormFile? LicensePhoto { get; set; }

        [Display(Name = "Vehicle Registration (OR/CR)")]
        public IFormFile? RegistrationFile { get; set; }
    }

    // ── My Permits (user side list) ───────────────────────────
    public class MyPermitViewModel
    {
        public int PermitId { get; set; }
        public int VehicleId { get; set; }
        public string? PlateNumber { get; set; }
        public string VehicleDisplay { get; set; } = string.Empty;
        public PermitType PermitType { get; set; }
        public PermitStatus Status { get; set; }
        public DateTime AppliedAt { get; set; }
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidUntil { get; set; }
        public string? Remarks { get; set; }

        // ✅ For showing document badges + view links on user side
        public bool HasLicensePhoto { get; set; }
        public bool HasRegistrationFile { get; set; }
    }

    // ── Admin Permit List ─────────────────────────────────────
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
        public bool HasLicensePhoto { get; set; }
        public bool HasRegistrationFile { get; set; }
    }

    // ── Admin Review ──────────────────────────────────────────
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

        // ✅ File info mapped from ParkingPermit
        public bool HasLicensePhoto { get; set; }
        public string? LicensePhotoFileName { get; set; }
        public bool HasRegistrationFile { get; set; }
        public string? RegistrationFileName { get; set; }

        [Display(Name = "Valid From")]
        public DateTime? ValidFrom { get; set; }

        [Display(Name = "Valid Until")]
        public DateTime? ValidUntil { get; set; }

        [StringLength(500)]
        [Display(Name = "Remarks (optional)")]
        public string? Remarks { get; set; }

        // Sprint 3 addition
        public bool HasQRCode { get; set; }

    }
}