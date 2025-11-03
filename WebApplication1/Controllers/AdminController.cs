using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            ILogger<AdminController> logger)
        {
            _userManager = userManager;
            _context = context;
            _logger = logger;
        }

        // GET: Admin/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var totalUsers = await _userManager.Users.CountAsync();
            var totalDoctors = await _context.Doctors.CountAsync();
            var verifiedDoctors = await _context.Doctors.CountAsync(d => d.IsVerified);
            var pendingDoctors = await _context.Doctors.CountAsync(d => !d.IsVerified);
            var totalMessages = await _context.Messages.CountAsync();
            var totalReviews = await _context.Reviews.CountAsync();

            ViewBag.TotalUsers = totalUsers;
            ViewBag.TotalDoctors = totalDoctors;
            ViewBag.VerifiedDoctors = verifiedDoctors;
            ViewBag.PendingDoctors = pendingDoctors;
            ViewBag.TotalMessages = totalMessages;
            ViewBag.TotalReviews = totalReviews;

            var pendingDoctorsList = await _context.Doctors
                .Where(d => !d.IsVerified)
                .Include(d => d.ApplicationUser)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            return View(pendingDoctorsList);
        }

        // POST: Admin/VerifyDoctor
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyDoctor(int doctorId)
        {
            try
            {
                var doctor = await _context.Doctors.FindAsync(doctorId);
                if (doctor == null)
                {
                    return Json(new { success = false, error = "Doctor not found." });
                }

                doctor.IsVerified = true;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Doctor {doctor.Email} verified by admin.");

                return Json(new { success = true, message = "Doctor verified successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying doctor");
                return Json(new { success = false, error = "Failed to verify doctor." });
            }
        }

        // POST: Admin/RejectDoctor
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectDoctor(int doctorId)
        {
            try
            {
                var doctor = await _context.Doctors
                    .Include(d => d.ApplicationUser)
                    .FirstOrDefaultAsync(d => d.Id == doctorId);

                if (doctor == null)
                {
                    return Json(new { success = false, error = "Doctor not found." });
                }

                // Delete doctor profile and user account
                if (doctor.ApplicationUser != null)
                {
                    await _userManager.DeleteAsync(doctor.ApplicationUser);
                }

                _context.Doctors.Remove(doctor);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Doctor {doctor.Email} rejected and deleted by admin.");

                return Json(new { success = true, message = "Doctor rejected and account deleted." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting doctor");
                return Json(new { success = false, error = "Failed to reject doctor." });
            }
        }

        // GET: Admin/Users
        public async Task<IActionResult> Users()
        {
            var users = await _userManager.Users
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            var userList = new List<dynamic>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userList.Add(new
                {
                    User = user,
                    Roles = roles
                });
            }

            return View(userList);
        }

        // GET: Admin/Doctors
        public async Task<IActionResult> Doctors()
        {
            var doctors = await _context.Doctors
                .Include(d => d.ApplicationUser)
                .Include(d => d.Reviews)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            return View(doctors);
        }
    }
}