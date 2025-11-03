using Microsoft.AspNetCore.Mvc.ViewEngines;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class Doctor
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Phone]
        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [StringLength(100)]
        public string? Specialization { get; set; }

        public int YearsOfExperience { get; set; }

        [StringLength(50)]
        public string? LicenseNumber { get; set; }

        [StringLength(500)]
        public string? ClinicAddress { get; set; }

        [StringLength(1000)]
        public string? Bio { get; set; }

        public bool IsVerified { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Foreign key to ApplicationUser (if doctor is also a user)
        public int? ApplicationUserId { get; set; }

        [ForeignKey("ApplicationUserId")]
        public virtual ApplicationUser? ApplicationUser { get; set; }

        // Navigation properties
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}
