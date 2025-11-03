using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace WebApplication1.Models
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Review> Reviews { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Doctor entity
            modelBuilder.Entity<Doctor>(entity =>
            {
                entity.HasKey(d => d.Id);
                
                entity.Property(d => d.FullName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(d => d.Email)
                    .IsRequired()
                    .HasMaxLength(256);

                entity.Property(d => d.Specialization)
                    .HasMaxLength(100);

                entity.Property(d => d.LicenseNumber)
                    .HasMaxLength(50);

                // One-to-One relationship between Doctor and ApplicationUser
                entity.HasOne(d => d.ApplicationUser)
                    .WithOne(u => u.Doctor)
                    .HasForeignKey<Doctor>(d => d.ApplicationUserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Configure Doctor-Reviews relationship
                entity.HasMany(d => d.Reviews)
                    .WithOne(r => r.Doctor)
                    .HasForeignKey(r => r.DoctorId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Message entity with Sender and Receiver
            modelBuilder.Entity<Message>(entity =>
            {
                entity.HasKey(m => m.Id);

                entity.Property(m => m.Content)
                    .IsRequired()
                    .HasMaxLength(2000);

                entity.Property(m => m.SentAt)
                    .IsRequired();

                // Configure Sender relationship
                entity.HasOne(m => m.Sender)
                    .WithMany()
                    .HasForeignKey(m => m.SenderId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Configure Receiver relationship
                entity.HasOne(m => m.Receiver)
                    .WithMany()
                    .HasForeignKey(m => m.ReceiverId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Create index for faster message queries
                entity.HasIndex(m => new { m.SenderId, m.ReceiverId });
                entity.HasIndex(m => m.SentAt);
            });

            // Configure Review entity
            modelBuilder.Entity<Review>(entity =>
            {
                entity.HasKey(r => r.Id);

                entity.Property(r => r.Rating)
                    .IsRequired();

                entity.Property(r => r.Comment)
                    .HasMaxLength(1000);

                entity.Property(r => r.CreatedAt)
                    .IsRequired();

                // Configure User-Reviews relationship
                entity.HasOne(r => r.User)
                    .WithMany(u => u.Reviews)
                    .HasForeignKey(r => r.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Configure Doctor-Reviews relationship
                entity.HasOne(r => r.Doctor)
                    .WithMany(d => d.Reviews)
                    .HasForeignKey(r => r.DoctorId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Prevent duplicate reviews from same user to same doctor
                entity.HasIndex(r => new { r.UserId, r.DoctorId })
                    .IsUnique();

                // Create index for rating queries
                entity.HasIndex(r => r.Rating);
            });

            // Configure ApplicationUser
            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(u => u.FullName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(u => u.CreatedAt)
                    .IsRequired();
            });
        }
    }
}
