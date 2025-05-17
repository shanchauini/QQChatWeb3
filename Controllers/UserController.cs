using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QQChatWeb3.Models;
using System.Security.Cryptography;
using System.Text;

namespace QQChatWeb3.Controllers
{
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserController> _logger;

        public UserController(ApplicationDbContext context, ILogger<UserController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: User/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: User/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(int userId, string password)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserId == userId && u.PSW == password);

                if (user == null)
                {
                    ModelState.AddModelError("", "用户ID或密码错误");
                    return View();
                }

                if (!user.IsApproved)
                {
                    ModelState.AddModelError("", "账号尚未通过审核");
                    return View();
                }

                if (user.IsBanned)
                {
                    ModelState.AddModelError("", "账号已被禁用");
                    return View();
                }

                HttpContext.Session.SetInt32("UserId", user.UserId);
                HttpContext.Session.SetString("UserName", user.UserName);

                _logger.LogInformation($"用户 {user.UserId} 登录成功");
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "登录过程中发生错误");
                ModelState.AddModelError("", "登录过程中发生错误，请稍后重试");
                return View();
            }
        }

        // GET: User/Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: User/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(User user)
        {
            if (ModelState.IsValid)
            {
                if (await _context.Users.AnyAsync(u => u.UserName == user.UserName))
                {
                    ModelState.AddModelError("UserName", "用户名已存在");
                    return View(user);
                }

                _context.Add(user);
                await _context.SaveChangesAsync();

                TempData["NewUserId"] = user.UserId;
                return RedirectToAction(nameof(RegisterSuccess));
            }
            return View(user);
        }

        // GET: User/RegisterSuccess
        public IActionResult RegisterSuccess()
        {
            ViewBag.NewUserId = TempData["NewUserId"];
            return View();
        }

        // GET: User/Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction(nameof(Login));
        }

        // GET: User/ManageInfo
        public async Task<IActionResult> ManageInfo()
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return RedirectToAction("Login");
                }

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound();
                }

                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户信息时发生错误");
                return RedirectToAction("Index", "Home");
            }
        }

        // POST: User/ManageInfo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageInfo(User model)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return RedirectToAction("Login");
                }

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound();
                }

                // 只更新允许修改的字段
                user.UserName = model.UserName;
                user.Gender = model.Gender;
                user.Email = model.Email;

                if (!string.IsNullOrEmpty(model.PSW))
                {
                    user.PSW = model.PSW;
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "个人信息更新成功";
                return RedirectToAction(nameof(ManageInfo));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新用户信息时发生错误");
                ModelState.AddModelError("", "更新信息失败，请稍后重试");
                return View(model);
            }
        }
    }
} 