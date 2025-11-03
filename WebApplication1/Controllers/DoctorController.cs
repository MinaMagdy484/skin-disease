using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class DoctorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<DoctorController> _logger;

        public DoctorController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<DoctorController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: Doctor/List
        public async Task<IActionResult> List()
        {
            var doctors = await _context.Doctors
                .Include(d => d.Reviews)
                    .ThenInclude(r => r.User)
                .ToListAsync();

            return View(doctors);
        }

        // GET: Doctor/Profile/5
        public async Task<IActionResult> Profile(int id)
        {
            var doctor = await _context.Doctors
                .Include(d => d.Reviews)
                    .ThenInclude(r => r.User)
                .Include(d => d.ApplicationUser)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doctor == null)
            {
                return NotFound();
            }

            // Check if current user already reviewed this doctor
            if (User.Identity?.IsAuthenticated == true)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser != null)
                {
                    ViewBag.HasReviewed = await _context.Reviews
                        .AnyAsync(r => r.UserId == currentUser.Id && r.DoctorId == id);
                    ViewBag.CurrentUserId = currentUser.Id;
                }
            }

            return View(doctor);
        }

        // GET: Doctor/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return RedirectToAction("Login", "Account");
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var doctor = await _context.Doctors
                .Include(d => d.Reviews)
                    .ThenInclude(r => r.User)
                .Include(d => d.ApplicationUser)
                .FirstOrDefaultAsync(d => d.ApplicationUserId == currentUser.Id);

            if (doctor == null)
            {
                return RedirectToAction("Index", "Home");
            }

            // Get recent messages
            var recentMessages = await _context.Messages
                .Where(m => m.ReceiverId == currentUser.Id || m.SenderId == currentUser.Id)
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .OrderByDescending(m => m.SentAt)
                .GroupBy(m => m.SenderId == currentUser.Id ? m.ReceiverId : m.SenderId)
                .Select(g => new
                {
                    OtherUserId = g.Key,
                    OtherUser = g.First().SenderId == currentUser.Id ? g.First().Receiver : g.First().Sender,
                    LastMessage = g.First(),
                    UnreadCount = g.Count(m => m.ReceiverId == currentUser.Id && !m.IsRead)
                })
                .Take(10)
                .ToListAsync();

            ViewBag.RecentMessages = recentMessages;
            ViewBag.TotalReviews = doctor.Reviews.Count;
            ViewBag.AverageRating = doctor.Reviews.Any() ? doctor.Reviews.Average(r => r.Rating) : 0;
            ViewBag.TotalPatients = recentMessages.Count;

            return View(doctor);
        }

        // POST: Doctor/AddReview
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(int doctorId, int rating, string comment)
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return Json(new { success = false, error = "Please login to leave a review." });
            }

            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Json(new { success = false, error = "User not found." });
                }

                var existingReview = await _context.Reviews
                    .FirstOrDefaultAsync(r => r.UserId == currentUser.Id && r.DoctorId == doctorId);

                if (existingReview != null)
                {
                    return Json(new { success = false, error = "You have already reviewed this doctor." });
                }

                var review = new Review
                {
                    UserId = currentUser.Id,
                    DoctorId = doctorId,
                    Rating = rating,
                    Comment = comment,
                    CreatedAt = DateTime.Now
                };

                _context.Reviews.Add(review);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"User {currentUser.Id} added review for doctor {doctorId}.");
                
                return Json(new { 
                    success = true, 
                    message = "Review added successfully!",
                    userName = currentUser.FullName,
                    date = review.CreatedAt.ToString("MMM dd, yyyy")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding review");
                return Json(new { success = false, error = "Failed to add review." });
            }
        }
    }
}