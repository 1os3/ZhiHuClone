using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZhihuClone.Core.Interfaces;
using ZhihuClone.Web.Models.User;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace ZhihuClone.Web.Controllers
{
    public class ProfileController : Controller
    {
        private readonly IUserService _userService;
        private readonly IPostService _postService;
        private readonly IMediaService _mediaService;

        public ProfileController(
            IUserService userService,
            IPostService postService,
            IMediaService mediaService)
        {
            _userService = userService;
            _postService = postService;
            _mediaService = mediaService;
        }

        [HttpGet("user/{username}")]
        public async Task<IActionResult> Index(string username)
        {
            var user = await _userService.GetByUsernameAsync(username);
            if (user == null)
                return NotFound();

            var viewModel = new UserProfileViewModel
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Nickname = user.Nickname,
                Avatar = user.Avatar,
                Bio = user.Bio,
                Location = user.Location,
                Company = user.Company,
                Title = user.Title,
                Website = user.Website,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };

            // 获取用户的文章列表
            viewModel.Posts = await _postService.GetUserPostsAsync(user.Id, 1, 10);

            return View(viewModel);
        }

        [Authorize]
        [HttpGet("settings")]
        public async Task<IActionResult> Settings()
        {
            var userId = int.Parse(User.FindFirst("nameid")?.Value ?? "0");
            if (userId == 0)
                return RedirectToAction("Login", "Account");

            var user = await _userService.GetByIdAsync(userId);
            if (user == null)
                return NotFound();

            var viewModel = new UserProfileViewModel
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Nickname = user.Nickname ?? string.Empty,
                Avatar = user.Avatar ?? string.Empty,
                Bio = user.Bio ?? string.Empty,
                Location = user.Location ?? string.Empty,
                Company = user.Company ?? string.Empty,
                Title = user.Title ?? string.Empty,
                Website = user.Website ?? string.Empty
            };

            return View(viewModel);
        }

        [Authorize]
        [HttpPost("settings")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Settings(UserProfileViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userId = int.Parse(User.FindFirst("nameid")?.Value ?? "0");
            if (userId == 0 || userId != model.Id)
                return RedirectToAction("Login", "Account");

            var user = await _userService.GetByIdAsync(userId);
            if (user == null)
                return NotFound();

            user.Nickname = model.Nickname;
            user.Bio = model.Bio;
            user.Location = model.Location;
            user.Company = model.Company;
            user.Title = model.Title;
            user.Website = model.Website;
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userService.UpdateAsync(user);
            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Errors.FirstOrDefault() ?? "保存失败");
                return View(model);
            }

            // 更新用户Claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Id.ToString()),
                new Claim("username", user.UserName ?? string.Empty),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.IsAdmin ? "Admin" : "User"),
                new Claim("nameid", user.Id.ToString()),
                new Claim("Avatar", user.Avatar ?? string.Empty),
                new Claim("Nickname", user.Nickname ?? user.UserName ?? string.Empty)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
            });

            return RedirectToAction(nameof(Index), new { username = user.UserName });
        }

        [Authorize]
        [HttpPost("settings/avatar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "请选择要上传的图片" });

            var userId = int.Parse(User.FindFirst("nameid")?.Value ?? "0");
            if (userId == 0)
                return Unauthorized();

            var user = await _userService.GetByIdAsync(userId);
            if (user == null)
                return NotFound();

            // 上传头像
            var avatarUrl = await _mediaService.UploadAvatarAsync(file);
            if (string.IsNullOrEmpty(avatarUrl))
                return BadRequest(new { error = "头像上传失败，请重试" });

            // 更新用户头像
            user.Avatar = avatarUrl;
            user.UpdatedAt = DateTime.UtcNow;
            await _userService.UpdateAsync(user);

            return Ok(new { avatarUrl });
        }

        [Authorize]
        [HttpPost("u/{username}/follow")]
        public async Task<IActionResult> Follow(string username)
        {
            var userId = int.Parse(User.FindFirst("nameid")?.Value ?? "0");
            if (userId == 0)
                return RedirectToAction("Login", "Account");

            var targetUser = await _userService.GetByUsernameAsync(username);
            if (targetUser == null)
                return NotFound();

            if (userId == targetUser.Id)
                return BadRequest(new { error = "不能关注自己" });

            var isFollowing = await _userService.IsFollowingAsync(userId, targetUser.Id);
            if (isFollowing)
            {
                await _userService.UnfollowAsync(userId, targetUser.Id);
                return Ok(new { isFollowing = false });
            }
            else
            {
                await _userService.FollowAsync(userId, targetUser.Id);
                return Ok(new { isFollowing = true });
            }
        }

        [Authorize]
        [HttpGet("u/{username}/following")]
        public async Task<IActionResult> Following(string username)
        {
            var user = await _userService.GetByUsernameAsync(username);
            if (user == null)
                return NotFound();

            var currentUserId = User.Identity?.IsAuthenticated == true ? 
                int.Parse(User.FindFirst("nameid")?.Value ?? "0") : 0;

            var following = await _userService.GetFollowingAsync(user.Id);
            var users = new List<UserItemViewModel>();

            foreach (var f in following)
            {
                users.Add(new UserItemViewModel
                {
                    Id = f.Id,
                    Username = f.UserName ?? string.Empty,
                    Nickname = f.Nickname ?? string.Empty,
                    Avatar = f.Avatar,
                    Bio = f.Bio,
                    Title = f.Title,
                    IsFollowing = currentUserId > 0 && await _userService.IsFollowingAsync(currentUserId, f.Id),
                    FollowerCount = await _userService.GetFollowerCountAsync(f.Id),
                    PostCount = await _postService.GetUserPostCountAsync(f.Id)
                });
            }

            var viewModel = new UserListViewModel
            {
                PageTitle = $"{user.Nickname}关注的人",
                Username = user.UserName ?? string.Empty,
                Nickname = user.Nickname ?? string.Empty,
                IsCurrentUser = currentUserId == user.Id,
                Users = users
            };

            return View("UserList", viewModel);
        }

        [Authorize]
        [HttpGet("u/{username}/followers")]
        public async Task<IActionResult> Followers(string username)
        {
            var user = await _userService.GetByUsernameAsync(username);
            if (user == null)
                return NotFound();

            var currentUserId = User.Identity?.IsAuthenticated == true ? 
                int.Parse(User.FindFirst("nameid")?.Value ?? "0") : 0;

            var followers = await _userService.GetFollowersAsync(user.Id);
            var users = new List<UserItemViewModel>();

            foreach (var f in followers)
            {
                users.Add(new UserItemViewModel
                {
                    Id = f.Id,
                    Username = f.UserName ?? string.Empty,
                    Nickname = f.Nickname ?? string.Empty,
                    Avatar = f.Avatar,
                    Bio = f.Bio,
                    Title = f.Title,
                    IsFollowing = currentUserId > 0 && await _userService.IsFollowingAsync(currentUserId, f.Id),
                    FollowerCount = await _userService.GetFollowerCountAsync(f.Id),
                    PostCount = await _postService.GetUserPostCountAsync(f.Id)
                });
            }

            var viewModel = new UserListViewModel
            {
                PageTitle = $"{user.Nickname}的关注者",
                Username = user.UserName ?? string.Empty,
                Nickname = user.Nickname ?? string.Empty,
                IsCurrentUser = currentUserId == user.Id,
                Users = users
            };

            return View("UserList", viewModel);
        }

        [Authorize]
        [HttpGet("settings/security")]
        public async Task<IActionResult> Security()
        {
            var userId = int.Parse(User.FindFirst("nameid")?.Value ?? "0");
            if (userId == 0)
                return RedirectToAction("Login", "Account");

            var user = await _userService.GetByIdAsync(userId);
            if (user == null)
                return NotFound();

            var viewModel = new SecuritySettingsViewModel
            {
                IsEmailConfirmed = user.EmailConfirmed,
                IsPhoneConfirmed = user.PhoneNumberConfirmed,
                IsTwoFactorEnabled = user.TwoFactorEnabled,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                LastLoginTime = user.LastLoginAt ?? DateTime.MinValue
            };

            return View(viewModel);
        }

        [Authorize]
        [HttpGet("settings/security/password")]
        public IActionResult Password()
        {
            return View();
        }

        [Authorize]
        [HttpPost("settings/security/password")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = int.Parse(User.FindFirst("nameid")?.Value ?? "0");
            if (userId == 0)
                return RedirectToAction("Login", "Account");

            var result = await _userService.ChangePasswordAsync(userId, model.CurrentPassword, model.NewPassword);
            if (!result.Succeeded)
            {
                return BadRequest(new { error = result.Errors.FirstOrDefault() ?? "修改密码失败" });
            }

            return Ok(new { message = "密码修改成功" });
        }

        [Authorize]
        [HttpPost("settings/security/email/send-code")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendEmailVerificationCode(VerifyEmailViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = int.Parse(User.FindFirst("nameid")?.Value ?? "0");
            if (userId == 0)
                return RedirectToAction("Login", "Account");

            var result = await _userService.SendEmailVerificationCodeAsync(userId, model.Email);
            if (!result.Succeeded)
            {
                return BadRequest(new { error = result.Errors.FirstOrDefault() ?? "发送验证码失败" });
            }

            return Ok(new { message = "验证码已发送到您的邮箱" });
        }

        [Authorize]
        [HttpPost("settings/security/email/verify")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyEmail(string code)
        {
            if (string.IsNullOrEmpty(code))
                return BadRequest(new { error = "请输入验证码" });

            var userId = int.Parse(User.FindFirst("nameid")?.Value ?? "0");
            if (userId == 0)
                return RedirectToAction("Login", "Account");

            var result = await _userService.VerifyEmailAsync(userId, code);
            if (!result.Succeeded)
            {
                return BadRequest(new { error = result.Errors.FirstOrDefault() ?? "验证失败" });
            }

            return Ok(new { message = "邮箱验证成功" });
        }

        [Authorize]
        [HttpPost("settings/security/phone/send-code")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendPhoneVerificationCode(VerifyPhoneViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = int.Parse(User.FindFirst("nameid")?.Value ?? "0");
            if (userId == 0)
                return RedirectToAction("Login", "Account");

            var result = await _userService.SendPhoneVerificationCodeAsync(userId, model.PhoneNumber);
            if (!result.Succeeded)
            {
                return BadRequest(new { error = result.Errors.FirstOrDefault() ?? "发送验证码失败" });
            }

            return Ok(new { message = "验证码已发送到您的手机" });
        }

        [Authorize]
        [HttpPost("settings/security/phone/verify")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyPhone(VerifyPhoneViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = int.Parse(User.FindFirst("nameid")?.Value ?? "0");
            if (userId == 0)
                return RedirectToAction("Login", "Account");

            var result = await _userService.VerifyPhoneNumberAsync(userId, model.PhoneNumber, model.VerificationCode);
            if (!result.Succeeded)
            {
                return BadRequest(new { error = result.Errors.FirstOrDefault() ?? "验证失败" });
            }

            return Ok(new { message = "手机验证成功" });
        }

        [Authorize]
        [HttpPost("settings/security/2fa")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleTwoFactorAuth()
        {
            var userId = int.Parse(User.FindFirst("nameid")?.Value ?? "0");
            if (userId == 0)
                return RedirectToAction("Login", "Account");

            var result = await _userService.ToggleTwoFactorAuthenticationAsync(userId);
            if (!result.Succeeded)
            {
                return BadRequest(new { error = result.Errors.FirstOrDefault() ?? "操作失败" });
            }

            return Ok(new { enabled = result.Data });
        }

        [Authorize]
        [HttpPost("settings/security/delete-account")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount(DeleteAccountViewModel model)
        {
            var userId = int.Parse(User.FindFirst("nameid")?.Value ?? "0");
            if (userId == 0)
                return RedirectToAction("Login", "Account");

            var result = await _userService.DeleteAccountAsync(userId);
            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Errors.FirstOrDefault() ?? "删除账号失败");
                return View("Security");
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        [HttpGet("settings/privacy")]
        public async Task<IActionResult> Privacy()
        {
            var userId = int.Parse(User.FindFirst("nameid")?.Value ?? "0");
            if (userId == 0)
                return RedirectToAction("Login", "Account");

            var user = await _userService.GetByIdAsync(userId);
            if (user == null)
                return NotFound();

            var viewModel = new PrivacySettingsViewModel
            {
                ShowEmail = user.ShowEmail,
                ShowPhone = user.ShowPhone,
                ShowLocation = user.ShowLocation,
                ShowCompany = user.ShowCompany,
                AllowFollow = user.AllowFollow,
                AllowDirectMessage = user.AllowDirectMessage,
                AllowNotification = user.AllowNotification
            };

            return View(viewModel);
        }

        [Authorize]
        [HttpGet("settings/notification")]
        public async Task<IActionResult> Notification()
        {
            var userId = int.Parse(User.FindFirst("nameid")?.Value ?? "0");
            if (userId == 0)
                return RedirectToAction("Login", "Account");

            var user = await _userService.GetByIdAsync(userId);
            if (user == null)
                return NotFound();

            var viewModel = new NotificationSettingsViewModel
            {
                EmailNotification = user.EmailNotification,
                PushNotification = user.PushNotification,
                LikeNotification = user.LikeNotification,
                CommentNotification = user.CommentNotification,
                FollowNotification = user.FollowNotification,
                SystemNotification = user.SystemNotification
            };

            return View(viewModel);
        }

        [Authorize]
        [HttpGet("settings/security/under-development")]
        public IActionResult UnderDevelopment()
        {
            return View();
        }

        [Authorize]
        [HttpGet("settings/security/email")]
        public IActionResult VerifyEmail()
        {
            return RedirectToAction(nameof(UnderDevelopment));
        }

        [Authorize]
        [HttpGet("settings/security/phone")]
        public IActionResult VerifyPhone()
        {
            return RedirectToAction(nameof(UnderDevelopment));
        }

        [Authorize]
        [HttpGet("settings/security/2fa")]
        public IActionResult TwoFactorAuth()
        {
            return RedirectToAction(nameof(UnderDevelopment));
        }

        [Authorize]
        [HttpPost("settings/privacy")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SavePrivacy(PrivacySettingsViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = int.Parse(User.FindFirst("nameid")?.Value ?? "0");
            if (userId == 0)
                return RedirectToAction("Login", "Account");

            var user = await _userService.GetByIdAsync(userId);
            if (user == null)
                return NotFound();

            user.ShowEmail = model.ShowEmail;
            user.ShowPhone = model.ShowPhone;
            user.ShowLocation = model.ShowLocation;
            user.ShowCompany = model.ShowCompany;
            user.AllowFollow = model.AllowFollow;
            user.AllowDirectMessage = model.AllowDirectMessage;
            user.AllowNotification = model.AllowNotification;
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userService.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(new { error = result.Errors.FirstOrDefault() ?? "保存失败" });
            }

            return Ok(new { message = "设置已保存" });
        }

        [Authorize]
        [HttpPost("settings/notification")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveNotification(NotificationSettingsViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = int.Parse(User.FindFirst("nameid")?.Value ?? "0");
            if (userId == 0)
                return RedirectToAction("Login", "Account");

            var user = await _userService.GetByIdAsync(userId);
            if (user == null)
                return NotFound();

            user.EmailNotification = model.EmailNotification;
            user.PushNotification = model.PushNotification;
            user.LikeNotification = model.LikeNotification;
            user.CommentNotification = model.CommentNotification;
            user.FollowNotification = model.FollowNotification;
            user.SystemNotification = model.SystemNotification;
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userService.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(new { error = result.Errors.FirstOrDefault() ?? "保存失败" });
            }

            return Ok(new { message = "设置已保存" });
        }
    }
} 