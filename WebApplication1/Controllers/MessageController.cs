using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class MessageController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<MessageController> _logger;

        public MessageController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<MessageController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: Message/Index
        public async Task<IActionResult> Index(int? userId)
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

            if (userId.HasValue)
            {
                var messages = await _context.Messages
                    .Where(m => (m.SenderId == currentUser.Id && m.ReceiverId == userId.Value) ||
                               (m.SenderId == userId.Value && m.ReceiverId == currentUser.Id))
                    .Include(m => m.Sender)
                    .Include(m => m.Receiver)
                    .OrderBy(m => m.SentAt)
                    .ToListAsync();

                // Mark messages as read
                var unreadMessages = messages.Where(m => m.ReceiverId == currentUser.Id && !m.IsRead);
                foreach (var msg in unreadMessages)
                {
                    msg.IsRead = true;
                }
                if (unreadMessages.Any())
                {
                    await _context.SaveChangesAsync();
                }

                var otherUser = await _userManager.FindByIdAsync(userId.Value.ToString());
                ViewBag.OtherUser = otherUser;
                ViewBag.CurrentUserId = currentUser.Id;

                return View("Chat", messages);
            }
            else
            {
                // Show list of conversations
                var conversations = await _context.Messages
                    .Where(m => m.SenderId == currentUser.Id || m.ReceiverId == currentUser.Id)
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
                    .ToListAsync();

                return View("Conversations", conversations);
            }
        }

        // POST: Message/Send
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(int receiverId, string content)
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return Json(new { success = false, error = "Please login to send messages." });
            }

            try
            {
                if (string.IsNullOrWhiteSpace(content))
                {
                    return Json(new { success = false, error = "Message cannot be empty." });
                }

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Json(new { success = false, error = "User not found." });
                }

                var message = new Message
                {
                    SenderId = currentUser.Id,
                    ReceiverId = receiverId,
                    Content = content,
                    SentAt = DateTime.Now,
                    IsRead = false
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Message sent from user {currentUser.Id} to user {receiverId}");

                return Json(new
                {
                    success = true,
                    message = "Message sent successfully!",
                    data = new
                    {
                        content = message.Content,
                        sentAt = message.SentAt.ToString("MMM dd, yyyy hh:mm tt"),
                        senderId = message.SenderId
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message");
                return Json(new { success = false, error = "Failed to send message." });
            }
        }

        // GET: Message/GetMessages
        [HttpGet]
        public async Task<IActionResult> GetMessages(int userId)
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return Json(new { success = false, error = "Please login." });
            }

            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Json(new { success = false, error = "User not found." });
                }

                var messages = await _context.Messages
                    .Where(m => (m.SenderId == currentUser.Id && m.ReceiverId == userId) ||
                               (m.SenderId == userId && m.ReceiverId == currentUser.Id))
                    .OrderBy(m => m.SentAt)
                    .Select(m => new
                    {
                        content = m.Content,
                        sentAt = m.SentAt.ToString("MMM dd, yyyy hh:mm tt"),
                        senderId = m.SenderId,
                        isRead = m.IsRead
                    })
                    .ToListAsync();

                // Mark as read
                var unreadMessages = await _context.Messages
                    .Where(m => m.SenderId == userId && m.ReceiverId == currentUser.Id && !m.IsRead)
                    .ToListAsync();

                foreach (var msg in unreadMessages)
                {
                    msg.IsRead = true;
                }

                if (unreadMessages.Any())
                {
                    await _context.SaveChangesAsync();
                }

                return Json(new { success = true, messages });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting messages");
                return Json(new { success = false, error = "Failed to load messages." });
            }
        }
    }
}