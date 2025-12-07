using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public PaymentController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);
            ViewBag.CurrentPredictions = user?.NumberOfPredictionValid ?? 0;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ProcessPayment([FromBody] PaymentRequest request)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var user = await _userManager.FindByIdAsync(userId);

                if (user == null)
                {
                    return Json(new { success = false, error = "User not found" });
                }

                // Add credits
                user.NumberOfPredictionValid += request.Credits;
                await _userManager.UpdateAsync(user);

                return Json(new { 
                    success = true, 
                    newBalance = user.NumberOfPredictionValid,
                    message = $"Successfully added {request.Credits} predictions!" 
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = "Payment processing failed" });
            }
        }
    }

    public class PaymentRequest
    {
        public string PlanName { get; set; }
        public int Credits { get; set; }
        public decimal Amount { get; set; }
    }
}