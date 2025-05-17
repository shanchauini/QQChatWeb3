using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QQChatWeb3.Models;
using System.Text.Json;

namespace QQChatWeb3.Controllers
{
    public class MessageController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<MessageController> _logger;

        public MessageController(ApplicationDbContext context, IWebHostEnvironment environment, ILogger<MessageController> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        // GET: Message/Chat/{friendId}
        public async Task<IActionResult> Chat(int friendId)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return RedirectToAction("Login", "User");
                }

                var messages = await _context.Messages
                    .Include(m => m.Sender)
                    .Include(m => m.File)
                    .Where(m => (m.SendId == userId && m.ReceiveId == friendId) ||
                                (m.SendId == friendId && m.ReceiveId == userId))
                    .OrderByDescending(m => m.SendTime)
                    .Take(50)
                    .ToListAsync();

                var friend = await _context.Users.FindAsync(friendId);
                if (friend == null)
                {
                    return NotFound();
                }

                ViewBag.FriendName = friend.UserName;
                _logger.LogInformation($"获取到 {messages.Count} 条消息");
                ViewBag.Friend = friend;
                return View(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取聊天消息时发生错误");
                return View(new List<Message>());
            }
        }

        // GET: Message/History
        public async Task<IActionResult> History(string? searchTerm = null)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return RedirectToAction("Login", "User");
                }

                var query = _context.Messages
                    .Include(m => m.Sender)
                    .Include(m => m.File)
                    .Where(m => m.SendId == userId || m.ReceiveId == userId);

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    query = query.Where(m => 
                        (m.Content != null && m.Content.Contains(searchTerm)) ||
                        (m.Sender != null && m.Sender.UserName.Contains(searchTerm)));
                }

                var messages = await query
                    .OrderByDescending(m => m.SendTime)
                    .ToListAsync();

                _logger.LogInformation($"获取到 {messages.Count} 条历史消息");
                return View(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取历史消息时发生错误");
                return View(new List<Message>());
            }
        }

        // POST: Message/Send
        [HttpPost]
        public async Task<IActionResult> Send(int receiverId, string content)
        {
            var senderId = HttpContext.Session.GetInt32("UserId");
            if (senderId == null)
            {
                return RedirectToAction("Login", "User");
            }

            var message = new Message
            {
                SendId = senderId.Value,
                ReceiveId = receiverId,
                Content = content,
                ReceiveType = "普通用户",
                MessageType = "Text",
                SendTime = DateTime.Now
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return Json(new { success = true, messageId = message.MessageId });
        }

        // POST: Message/UploadFile
        [HttpPost]
        public async Task<IActionResult> UploadFile(int receiverId, IFormFile file)
        {
            var senderId = HttpContext.Session.GetInt32("UserId");
            if (senderId == null)
            {
                return RedirectToAction("Login", "User");
            }

            if (file == null || file.Length == 0)
            {
                return Json(new { success = false, message = "没有选择文件" });
            }

            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var fileRecord = new ChatFile
            {
                FileName = file.FileName,
                FilePath = "/uploads/" + uniqueFileName,
                UploadDate = DateTime.Now
            };

            _context.Files.Add(fileRecord);
            await _context.SaveChangesAsync();

            var message = new Message
            {
                SendId = senderId.Value,
                ReceiveId = receiverId,
                Content = file.FileName,
                ReceiveType = "普通用户",
                MessageType = "File",
                FileId = fileRecord.FileId,
                SendTime = DateTime.Now
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return Json(new { success = true, messageId = message.MessageId });
        }

        // POST: Message/Delete
        [HttpPost]
        public async Task<IActionResult> Delete(int messageId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "User");
            }

            var message = await _context.Messages
                .FirstOrDefaultAsync(m => m.MessageId == messageId && 
                    (m.SendId == userId || m.ReceiveId == userId));

            if (message != null)
            {
                _context.Messages.Remove(message);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(History));
        }
    }
} 