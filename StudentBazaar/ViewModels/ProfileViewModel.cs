

namespace StudentBazaar.Web.ViewModels
{
    public class ProfileViewModel
    {
        [Required]
        [MaxLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [MaxLength(250)]
        [Display(Name = "Address")]
        public string? Address { get; set; }

        [Display(Name = "University")]
        public int? UniversityId { get; set; }

        [Display(Name = "College")]
        public int? CollegeId { get; set; }

        [Display(Name = "Profile Picture")]
        public string? ProfilePictureUrl { get; set; }

        public List<SelectListItem> Universities { get; set; } = new();
        public List<SelectListItem> Colleges { get; set; } = new();

        // Password change fields
        [Display(Name = "Current Password")]
        [DataType(DataType.Password)]
        public string? CurrentPassword { get; set; }

        [Display(Name = "New Password")]
        [DataType(DataType.Password)]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        public string? NewPassword { get; set; }

        [Display(Name = "Confirm New Password")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string? ConfirmPassword { get; set; }
    }
}

