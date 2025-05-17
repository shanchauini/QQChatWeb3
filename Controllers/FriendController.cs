using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QQChatWeb3.Models;

namespace QQChatWeb3.Controllers
{
    public class FriendController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FriendController> _logger;

        public FriendController(ApplicationDbContext context, ILogger<FriendController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Friend
        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return RedirectToAction("Login", "User");
                }

                var friendships = await _context.Friendships
                    .Include(f => f.User1)
                    .Include(f => f.User2)
                    .Where(f => f.UserId1 == userId || f.UserId2 == userId)
                    .ToListAsync();

                var friends = new List<User>();
                foreach (var friendship in friendships)
                {
                    if (friendship.User1 != null && friendship.User1.UserId != userId)
                    {
                        friends.Add(friendship.User1);
                    }
                    else if (friendship.User2 != null && friendship.User2.UserId != userId)
                    {
                        friends.Add(friendship.User2);
                    }
                }

                _logger.LogInformation($"获取到 {friends.Count} 个好友");
                return View(friends);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取好友列表时发生错误");
                return View(new List<User>());
            }
        }

        // GET: Friend/Search
        public async Task<IActionResult> Search(string? username)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return RedirectToAction("Login", "User");
                }

                var query = _context.Users.Where(u => u.UserId != userId);

                if (!string.IsNullOrEmpty(username))
                {
                    query = query.Where(u => u.UserName != null && u.UserName.Contains(username));
                }

                var users = await query.ToListAsync();
                _logger.LogInformation($"搜索到 {users.Count} 个用户");
                return View(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索用户时发生错误");
                return View(new List<User>());
            }
        }

        // POST: Friend/SendRequest
        [HttpPost]
        public async Task<IActionResult> SendRequest(int receiverId)
        {
            try
            {
                var senderId = HttpContext.Session.GetInt32("UserId");
                if (senderId == null)
                {
                    return RedirectToAction("Login", "User");
                }

                if (await _context.FriendRequests
                    .AnyAsync(r => r.SendId == senderId && r.ReceiveId == receiverId && r.RequestStatus == "申请中"))
                {
                    return Json(new { success = false, message = "已经发送过好友申请" });
                }

                var request = new FriendRequest
                {
                    SendId = senderId.Value,
                    ReceiveId = receiverId,
                    RequestStatus = "申请中",
                    RequestDate = DateTime.Now
                };

                _context.FriendRequests.Add(request);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"发送好友申请成功");
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送好友申请时发生错误");
                return Json(new { success = false, message = "发送好友申请失败" });
            }
        }

        // GET: Friend/Requests
        public async Task<IActionResult> Requests()
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return RedirectToAction("Login", "User");
                }

                var requests = await _context.FriendRequests
                    .Include(r => r.Sender)
                    .Where(r => r.ReceiveId == userId && r.RequestStatus == "申请中")
                    .OrderByDescending(r => r.RequestDate)
                    .ToListAsync();

                _logger.LogInformation($"获取到 {requests.Count} 条好友申请");
                return View(requests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取好友申请列表时发生错误");
                return View(new List<FriendRequest>());
            }
        }

        // POST: Friend/HandleRequest
        [HttpPost]
        public async Task<IActionResult> HandleRequest(int requestId, bool accept)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return RedirectToAction("Login", "User");
                }

                var request = await _context.FriendRequests
                    .FirstOrDefaultAsync(r => r.RequestId == requestId && r.ReceiveId == userId);

                if (request == null)
                {
                    return NotFound();
                }

                request.RequestStatus = accept ? "已接受" : "已拒绝";
                await _context.SaveChangesAsync();

                if (accept)
                {
                    var friendship = new Friendship
                    {
                        UserId1 = request.SendId,
                        UserId2 = userId.Value,
                        CreateDate = DateTime.Now
                    };

                    _context.Friendships.Add(friendship);
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation($"处理好友申请成功: {(accept ? "接受" : "拒绝")}");
                return RedirectToAction(nameof(Requests));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理好友申请时发生错误");
                return RedirectToAction(nameof(Requests));
            }
        }

        // POST: Friend/Delete
        [HttpPost]
        public async Task<IActionResult> Delete(int friendId)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return RedirectToAction("Login", "User");
                }

                var friendship = await _context.Friendships
                    .FirstOrDefaultAsync(f => 
                        (f.UserId1 == userId && f.UserId2 == friendId) ||
                        (f.UserId1 == friendId && f.UserId2 == userId));

                if (friendship != null)
                {
                    _context.Friendships.Remove(friendship);
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation($"删除好友成功");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除好友时发生错误");
                return RedirectToAction(nameof(Index));
            }
        }
    }
} 