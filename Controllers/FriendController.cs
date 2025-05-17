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

        // GET: Friend/Index
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
                    .Where(f => f.UserId1 == userId || f.UserId2 == userId)
                    .Include(f => f.User1)
                    .Include(f => f.User2)
                    .ToListAsync();

                var friends = friendships.Select(f => 
                    f.UserId1 == userId ? f.User2 : f.User1).ToList();

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
        public IActionResult Search()
        {
            return View();
        }

        // POST: Friend/Search
        [HttpPost]
        public async Task<IActionResult> Search(string searchTerm)
        {
            try
            {
                var users = await _context.Users
                    .Where(u => u.UserName.Contains(searchTerm))
                    .ToListAsync();

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

                var existingRequest = await _context.FriendRequests
                    .FirstOrDefaultAsync(fr => 
                        fr.SendId == senderId && 
                        fr.ReceiveId == receiverId && 
                        fr.RequestStatus == "申请中");

                if (existingRequest != null)
                {
                    return Json(new { success = false, message = "已经发送过好友申请" });
                }

                var friendRequest = new FriendRequest
                {
                    SendId = senderId.Value,
                    ReceiveId = receiverId,
                    RequestStatus = "申请中",
                    RequestDate = DateTime.Now
                };

                _context.FriendRequests.Add(friendRequest);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"用户 {senderId} 向用户 {receiverId} 发送了好友申请");
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送好友申请时发生错误");
                return Json(new { success = false, message = "发送申请失败，请稍后重试" });
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
                    .Where(fr => fr.ReceiveId == userId && fr.RequestStatus == "申请中")
                    .Include(fr => fr.Sender)
                    .ToListAsync();

                _logger.LogInformation($"获取到 {requests.Count} 个好友申请");
                return View(requests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取好友申请时发生错误");
                return View(new List<FriendRequest>());
            }
        }

        // POST: Friend/HandleRequest
        [HttpPost]
        public async Task<IActionResult> HandleRequest(int requestId, bool accept)
        {
            try
            {
                var request = await _context.FriendRequests
                    .Include(fr => fr.Sender)
                    .FirstOrDefaultAsync(fr => fr.RequestId == requestId);

                if (request == null)
                {
                    return Json(new { success = false, message = "请求不存在" });
                }

                request.RequestStatus = accept ? "已接受" : "已拒绝";

                if (accept)
                {
                    var friendship = new Friendship
                    {
                        UserId1 = request.SendId,
                        UserId2 = request.ReceiveId,
                        CreateDate = DateTime.Now
                    };

                    _context.Friendships.Add(friendship);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"用户 {request.ReceiveId} {(accept ? "接受" : "拒绝")}了用户 {request.SendId} 的好友申请");
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理好友申请时发生错误");
                return Json(new { success = false, message = "操作失败，请稍后重试" });
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
                    _logger.LogInformation($"用户 {userId} 删除了与用户 {friendId} 的好友关系");
                }

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