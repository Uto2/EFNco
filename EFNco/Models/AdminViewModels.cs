using System.ComponentModel.DataAnnotations;

namespace EFNco.Models
{
    public class UserListViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Department { get; set; }
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class EditUserViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string? Department { get; set; }
        public string? PhoneNumber { get; set; }
        public bool IsActive { get; set; }

        public string CurrentRole { get; set; } = string.Empty;
        public string? SelectedRole { get; set; }
        public List<string> AvailableRoles { get; set; } = new();
    }
}
