using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole<int>> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole<int>> roleManager,
            ApplicationDbContext context,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _context = context;
            _logger = logger;
        }

        // GET: Account/Login
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, bool rememberMe = false)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                TempData["Error"] = "Please enter both email and password.";
                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);

            if (user != null)
            {
                var result = await _signInManager.PasswordSignInAsync(user, password, rememberMe, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    var roles = await _userManager.GetRolesAsync(user);

                    // Check if user is admin
                    if (roles.Contains("Admin"))
                    {
                        HttpContext.Session.SetString("UserType", "Admin");
                        _logger.LogInformation($"Admin {email} logged in successfully.");
                        return RedirectToAction("Dashboard", "Admin");
                    }

                    // Check if user is a doctor
                    var doctor = await _context.Doctors
                        .FirstOrDefaultAsync(d => d.ApplicationUserId == user.Id);

                    if (doctor != null && roles.Contains("Doctor"))
                    {
                        HttpContext.Session.SetString("DoctorId", doctor.Id.ToString());
                        HttpContext.Session.SetString("UserType", "Doctor");
                        _logger.LogInformation($"Doctor {email} logged in successfully.");
                        return RedirectToAction("Dashboard", "Doctor");
                    }

                    // Regular user
                    HttpContext.Session.SetString("UserId", user.Id.ToString());
                    HttpContext.Session.SetString("UserType", "User");
                    _logger.LogInformation($"User {email} logged in successfully.");
                    return RedirectToAction("Index", "Home");
                }

                if (result.IsLockedOut)
                {
                    TempData["Error"] = "Account is locked out.";
                    return View();
                }
            }

            TempData["Error"] = "Invalid email or password.";
            return View();
        }

        // GET: Account/Register
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // POST: Account/RegisterUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterUser(string fullName, string email, string phoneNumber, DateTime? dateOfBirth, string address, string password)
        {
            try
            {
                if (await _userManager.FindByEmailAsync(email) != null)
                {
                    TempData["Error"] = "Email already exists.";
                    return RedirectToAction("Register");
                }

                var user = new ApplicationUser
                {
                    FullName = fullName,
                    Email = email,
                    UserName = email,
                    PhoneNumber = phoneNumber,
                    DateOfBirth = dateOfBirth,
                    Address = address,
                    CreatedAt = DateTime.Now
                };

                var result = await _userManager.CreateAsync(user, password);

                if (result.Succeeded)
                {
                    // Assign User role
                    await _userManager.AddToRoleAsync(user, "User");

                    _logger.LogInformation($"User {email} registered successfully with User role.");
                    TempData["Success"] = "Registration successful! Please login.";
                    return RedirectToAction("Login");
                }

                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                TempData["Error"] = $"Registration failed: {errors}";
                return RedirectToAction("Register");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering user");
                TempData["Error"] = "Registration failed. Please try again.";
                return RedirectToAction("Register");
            }
        }

        // POST: Account/RegisterDoctor
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterDoctor(string fullName, string email, string phoneNumber, string specialization, 
            int yearsOfExperience, string licenseNumber, string clinicAddress, string bio, string password)
        {
            try
            {
                if (await _userManager.FindByEmailAsync(email) != null)
                {
                    TempData["Error"] = "Email already exists.";
                    return RedirectToAction("Register");
                }

                // Create ApplicationUser first
                var user = new ApplicationUser
                {
                    FullName = fullName,
                    Email = email,
                    UserName = email,
                    PhoneNumber = phoneNumber,
                    CreatedAt = DateTime.Now
                };

                var result = await _userManager.CreateAsync(user, password);

                if (result.Succeeded)
                {
                    // Assign Doctor role
                    await _userManager.AddToRoleAsync(user, "Doctor");

                    // Create Doctor profile linked to the user
                    var doctor = new Doctor
                    {
                        FullName = fullName,
                        Email = email,
                        PasswordHash = _userManager.PasswordHasher.HashPassword(user, password),
                        PhoneNumber = phoneNumber,
                        Specialization = specialization,
                        YearsOfExperience = yearsOfExperience,
                        LicenseNumber = licenseNumber,
                        ClinicAddress = clinicAddress,
                        Bio = bio,
                        ApplicationUserId = user.Id,
                        IsVerified = false,
                        CreatedAt = DateTime.Now
                    };

                    _context.Doctors.Add(doctor);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"Doctor {email} registered successfully with Doctor role.");
                    TempData["Success"] = "Doctor registration successful! Your account is pending verification. Please login.";
                    return RedirectToAction("Login");
                }

                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                TempData["Error"] = $"Registration failed: {errors}";
                return RedirectToAction("Register");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering doctor");
                TempData["Error"] = "Registration failed. Please try again.";
                return RedirectToAction("Register");
            }
        }

        // GET: Account/Logout
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            HttpContext.Session.Clear();
            _logger.LogInformation("User logged out.");
            return RedirectToAction("Index", "Home");
        }

        // GET: Account/AccessDenied
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}