using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class ApplicationUser : IdentityUser<int>
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Address { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation property - if user is a doctor, this will NOT be null
        public virtual Doctor? Doctor { get; set; }

        // Navigation properties for Reviews
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}
