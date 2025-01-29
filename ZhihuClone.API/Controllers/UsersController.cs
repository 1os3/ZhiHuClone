using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZhihuClone.Core.Models;
using ZhihuClone.Core.Interfaces;
using ZhihuClone.Core.Interfaces.Security;
using System.ComponentModel.DataAnnotations;

namespace ZhihuClone.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ITokenGenerator _tokenGenerator;
        private readonly IFirewallService _firewallService;

        public UsersController(
            IUserService userService,
            ITokenGenerator tokenGenerator,
            IFirewallService firewallService)
        {
            _userService = userService;
            _tokenGenerator = tokenGenerator;
            _firewallService = firewallService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                if (!await _firewallService.CheckRateLimitAsync(ipAddress, "register"))
                {
                    return StatusCode(429, "注册请求过于频繁，请稍后再试");
                }

                if (await _firewallService.ContainsSensitiveContentAsync(request.Username) ||
                    await _firewallService.ContainsSensitiveContentAsync(request.Email))
                {
                    await _firewallService.LogAccessAsync(ipAddress, "register", false);
                    return BadRequest("包含不允许的内容");
                }

                var user = new User
                {
                    Email = request.Email,
                    UserName = request.Username,
                    Nickname = request.Nickname ?? request.Username,
                    IsEmailConfirmed = false,
                    CreatedAt = DateTime.UtcNow
                };

                if (await _userService.IsUsernameTakenAsync(request.Username))
                {
                    return BadRequest("用户名已被使用");
                }

                var createdUser = await _userService.CreateUserAsync(user, request.Password);
                var token = _tokenGenerator.GenerateToken(createdUser.Id, createdUser.UserName, createdUser.IsAdmin);

                await _firewallService.LogAccessAsync(ipAddress, "register", true);

                return Ok(new
                {
                    createdUser.Id,
                    createdUser.UserName,
                    createdUser.Email,
                    createdUser.Avatar,
                    Token = token
                });
            }
            catch (Exception)
            {
                return StatusCode(500, "注册时发生错误");
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                if (!await _firewallService.CheckRateLimitAsync(ipAddress, "login"))
                {
                    return StatusCode(429, "登录请求过于频繁，请稍后再试");
                }

                var user = await _userService.GetByUsernameAsync(request.Username);
                var validationResult = await _userService.ValidatePasswordAsync(user.Id, request.Password);
                if (user == null || !validationResult.Succeeded)
                {
                    await _firewallService.LogAccessAsync(ipAddress, "login", false);
                    return Unauthorized("用户名或密码错误");
                }

                if (!user.IsActive)
                {
                    return BadRequest("账号已被禁用");
                }

                var token = _tokenGenerator.GenerateToken(user.Id, user.UserName, user.IsAdmin);
                await _firewallService.LogAccessAsync(ipAddress, "login", true);

                return Ok(new
                {
                    user.Id,
                    user.UserName,
                    user.Email,
                    user.Avatar,
                    Token = token
                });
            }
            catch (Exception)
            {
                return StatusCode(500, "登录时发生错误");
            }
        }

        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var nameIdClaim = User.FindFirst("nameid")?.Value ?? throw new InvalidOperationException("用户ID不存在");
                var userId = int.Parse(nameIdClaim);
                var user = await _userService.GetByIdAsync(userId);

                if (user == null)
                    return NotFound("用户不存在");

                return Ok(new
                {
                    user.Id,
                    user.UserName,
                    user.Email,
                    user.Avatar,
                    user.Bio,
                    user.CreatedAt,
                    user.LastLoginAt,
                    user.IsAdmin
                });
            }
            catch (Exception)
            {
                return StatusCode(500, "获取用户信息时发生错误");
            }
        }

        [Authorize]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            try
            {
                var nameIdClaim = User.FindFirst("nameid")?.Value ?? throw new InvalidOperationException("用户ID不存在");
                var userId = int.Parse(nameIdClaim);
                var user = await _userService.GetByIdAsync(userId);

                if (user == null)
                    return NotFound("用户不存在");

                if (await _firewallService.ContainsSensitiveContentAsync(request.Bio))
                {
                    return BadRequest("个人简介包含不允许的内容");
                }

                var success = await _userService.UpdateUserProfileAsync(userId, request.Bio, request.Avatar);
                if (!success)
                    return BadRequest("更新个人信息失败");

                return Ok(new { Message = "个人信息更新成功" });
            }
            catch (Exception)
            {
                return StatusCode(500, "更新个人信息时发生错误");
            }
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                var nameIdClaim = User.FindFirst("nameid")?.Value ?? throw new InvalidOperationException("用户ID不存在");
                var userId = int.Parse(nameIdClaim);
                var result = await _userService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword);

                if (!result.Succeeded)
                    return BadRequest("当前密码错误");

                return Ok(new { Message = "密码修改成功" });
            }
            catch (Exception)
            {
                return StatusCode(500, "修改密码时发生错误");
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                return Ok(users);
            }
            catch (Exception)
            {
                return StatusCode(500, "获取用户列表时发生错误");
            }
        }
    }

    public class RegisterRequest
    {
        [Required]
        public string Username { get; set; } = string.Empty;
        [Required]
        public string Email { get; set; } = string.Empty;
        [Required]
        public string Password { get; set; } = string.Empty;
        public string? Nickname { get; set; }
    }

    public class LoginRequest
    {
        [Required]
        public string Username { get; set; } = string.Empty;
        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class UpdateProfileRequest
    {
        public string Bio { get; set; } = string.Empty;
        public string Avatar { get; set; } = string.Empty;
    }

    public class ChangePasswordRequest
    {
        [Required]
        public string CurrentPassword { get; set; } = string.Empty;
        [Required]
        public string NewPassword { get; set; } = string.Empty;
    }
} 