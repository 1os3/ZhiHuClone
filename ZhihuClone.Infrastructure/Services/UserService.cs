using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using ZhihuClone.Core.Interfaces;
using ZhihuClone.Core.Models;
using ZhihuClone.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;

namespace ZhihuClone.Infrastructure.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly Dictionary<int, string> _refreshTokens;
        private readonly UserManager<User> _userManager;
        private readonly IUserRepository _userRepository;

        public UserService(
            ApplicationDbContext context,
            IPasswordHasher passwordHasher,
            UserManager<User> userManager,
            IUserRepository userRepository)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _refreshTokens = new Dictionary<int, string>();
            _userManager = userManager;
            _userRepository = userRepository;
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == username);
        }

        public async Task<User> CreateUserAsync(User user, string password)
        {
            user.CreatedAt = DateTime.UtcNow;
            var result = await _userManager.CreateAsync(user, password);
            
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
            }
            
            return user;
        }

        public async Task<User> UpdateUserAsync(User user)
        {
            user.UpdatedAt = DateTime.UtcNow;
            var result = await _userManager.UpdateAsync(user);
            
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
            }
            
            return user;
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return false;

            user.IsDeleted = true;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<ServiceResult> ValidatePasswordAsync(int userId, string password)
        {
            var user = await GetByIdAsync(userId);
            if (user == null)
            {
                return new ServiceResult 
                { 
                    Succeeded = false,
                    Errors = new List<string> { "用户不存在" }
                };
            }

            var result = await _userManager.CheckPasswordAsync(user, password);
            return new ServiceResult 
            { 
                Succeeded = result,
                Errors = result ? new List<string>() : new List<string> { "密码错误" }
            };
        }

        public async Task<ServiceResult> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            var user = await GetByIdAsync(userId);
            if (user == null)
            {
                return new ServiceResult 
                { 
                    Succeeded = false,
                    Errors = new List<string> { "用户不存在" }
                };
            }

            var validateResult = await ValidatePasswordAsync(userId, currentPassword);
            if (!validateResult.Succeeded)
            {
                return new ServiceResult 
                { 
                    Succeeded = false,
                    Errors = new List<string> { "当前密码错误" }
                };
            }

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            return new ServiceResult 
            { 
                Succeeded = result.Succeeded,
                Errors = result.Errors.Select(e => e.Description).ToList()
            };
        }

        public async Task<bool> ResetPasswordAsync(int userId, string token, string newPassword)
        {
            var user = await GetByIdAsync(userId);
            if (user == null)
                return false;

            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
            return result.Succeeded;
        }

        public async Task<string> GeneratePasswordResetTokenAsync(int userId)
        {
            var user = await GetByIdAsync(userId);
            if (user == null)
                throw new ArgumentException("用户不存在");

            return await _userManager.GeneratePasswordResetTokenAsync(user);
        }

        public async Task<bool> IsEmailConfirmedAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            return user?.IsEmailConfirmed ?? false;
        }

        public async Task<string> GenerateEmailConfirmationTokenAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new ArgumentException("User not found");

            // TODO: Generate and store token
            return Guid.NewGuid().ToString();
        }

        public async Task<bool> ConfirmEmailAsync(int userId, string token)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            // TODO: Validate token
            user.IsEmailConfirmed = true;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<User>> SearchUsersAsync(string query, int page = 1, int pageSize = 10)
        {
            return await _context.Users
                .Where(u => u.UserName.Contains(query) || u.Email.Contains(query))
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<bool> IsUsernameAvailableAsync(string username)
        {
            return !await _context.Users.AnyAsync(u => u.UserName == username);
        }

        public async Task<bool> IsEmailAvailableAsync(string email)
        {
            return !await _context.Users.AnyAsync(u => u.Email == email);
        }

        public async Task<bool> IsEmailUniqueAsync(string email)
        {
            return !await _context.Users.AnyAsync(u => u.Email == email);
        }

        public async Task<bool> IsUsernameUniqueAsync(string username)
        {
            return !await _context.Users.AnyAsync(u => u.UserName == username);
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            return await _userManager.FindByIdAsync(id.ToString());
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _userManager.FindByNameAsync(username);
        }

        public async Task<ServiceResult> DeleteAccountAsync(int userId)
        {
            var user = await GetByIdAsync(userId);
            if (user == null)
            {
                return new ServiceResult 
                { 
                    Succeeded = false,
                    Errors = new List<string> { "用户不存在" }
                };
            }

            // 将用户标记为已删除
            user.IsDeleted = true;
            user.Email = string.Empty;
            user.PhoneNumber = string.Empty;
            user.UserName = $"deleted_{userId}_{DateTime.UtcNow.Ticks}";
            user.Nickname = "已注销用户";
            user.Avatar = string.Empty;
            user.Bio = string.Empty;
            user.Location = string.Empty;
            user.Company = string.Empty;
            user.Title = string.Empty;
            user.Website = string.Empty;

            var result = await _userManager.UpdateAsync(user);
            return new ServiceResult 
            { 
                Succeeded = result.Succeeded,
                Errors = result.Errors.Select(e => e.Description).ToList()
            };
        }

        public async Task<ServiceResult> UpdateAsync(User user)
        {
            var result = await _userManager.UpdateAsync(user);
            return new ServiceResult 
            { 
                Succeeded = result.Succeeded,
                Errors = result.Errors.Select(e => e.Description).ToList()
            };
        }

        public async Task<string> UpdateAvatarAsync(int userId, IFormFile avatar)
        {
            // TODO: Implement file upload
            var fileName = $"{userId}_{DateTime.UtcNow.Ticks}{Path.GetExtension(avatar.FileName)}";
            var filePath = Path.Combine("wwwroot", "uploads", "avatars", fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await avatar.CopyToAsync(stream);
            }

            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.Avatar = $"/uploads/avatars/{fileName}";
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return fileName;
        }

        public async Task<bool> UpdateUserProfileAsync(int userId, string bio, string avatar)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            user.Bio = bio;
            user.Avatar = avatar;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _context.Users
                .Where(u => !u.IsDeleted)
                .ToListAsync();
        }

        public void StoreRefreshToken(int userId, string refreshToken)
        {
            if (_refreshTokens.ContainsKey(userId))
            {
                _refreshTokens[userId] = refreshToken;
            }
            else
            {
                _refreshTokens.Add(userId, refreshToken);
            }
        }

        public void RemoveRefreshToken(int userId)
        {
            if (_refreshTokens.ContainsKey(userId))
            {
                _refreshTokens.Remove(userId);
            }
        }

        public bool ValidateRefreshToken(int userId, string refreshToken)
        {
            return _refreshTokens.TryGetValue(userId, out var storedToken) && storedToken == refreshToken;
        }

        public async Task<bool> IsEmailTakenAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }

        public async Task<bool> IsUsernameTakenAsync(string username)
        {
            return await _context.Users.AnyAsync(u => u.UserName == username);
        }

        public async Task<List<User>> GetAllAsync()
        {
            return await _context.Users
                .Where(u => !u.IsDeleted)
                .OrderBy(u => u.UserName)
                .ToListAsync();
        }

        public async Task<List<User>> GetPagedAsync(int page = 1, int pageSize = 20)
        {
            return await _context.Users
                .Where(u => !u.IsDeleted)
                .OrderBy(u => u.UserName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<User> CreateAsync(User user)
        {
            user.CreatedAt = DateTime.UtcNow;
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task DeleteAsync(int id)
        {
            var user = await GetByIdAsync(id);
            if (user != null)
            {
                user.IsDeleted = true;
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> FollowUserAsync(int followerId, int followedId)
        {
            var follower = await GetByIdAsync(followerId);
            var followed = await GetByIdAsync(followedId);

            if (follower == null || followed == null || followerId == followedId)
                return false;

            if (await IsFollowingAsync(followerId, followedId))
                return false;

            follower.Following.Add(followed);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UnfollowUserAsync(int followerId, int followedId)
        {
            var follower = await GetByIdAsync(followerId);
            var followed = await GetByIdAsync(followedId);

            if (follower == null || followed == null)
                return false;

            if (!await IsFollowingAsync(followerId, followedId))
                return false;

            follower.Following.Remove(followed);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsFollowingAsync(int followerId, int followedId)
        {
            var follower = await _context.Users
                .Include(u => u.Following)
                .FirstOrDefaultAsync(u => u.Id == followerId);

            return follower?.Following.Any(f => f.Id == followedId) ?? false;
        }

        public async Task<List<User>> GetFollowersAsync(int userId)
        {
            var user = await _userManager.Users
                .Include(u => u.Followers)
                .FirstOrDefaultAsync(u => u.Id == userId);

            return user?.Followers.ToList() ?? new List<User>();
        }

        public async Task<List<User>> GetFollowingAsync(int userId, int page = 1, int pageSize = 20)
        {
            var user = await _context.Users
                .Include(u => u.Following)
                .FirstOrDefaultAsync(u => u.Id == userId);

            return user?.Following
                .OrderBy(f => f.UserName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList() ?? new List<User>();
        }

        public async Task<int> GetFollowersCountAsync(int userId)
        {
            return await _context.Users
                .Where(u => u.Following.Any(f => f.Id == userId))
                .CountAsync();
        }

        public async Task<int> GetFollowingCountAsync(int userId)
        {
            return await _context.Users
                .Where(u => u.Followers.Any(f => f.Id == userId))
                .CountAsync();
        }

        public async Task<bool> IsLockedOutAsync(int userId)
        {
            var user = await GetByIdAsync(userId);
            return user?.LockoutEnd > DateTime.UtcNow;
        }

        public async Task<bool> LockoutAsync(int userId)
        {
            var user = await GetByIdAsync(userId);
            if (user == null)
                return false;

            user.LockoutEnd = DateTime.UtcNow.AddDays(7);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UnlockAsync(int userId)
        {
            var user = await GetByIdAsync(userId);
            if (user == null)
                return false;

            user.LockoutEnd = null;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsActiveAsync(int userId)
        {
            var user = await GetByIdAsync(userId);
            return user?.IsActive ?? false;
        }

        public async Task<bool> ActivateAsync(int userId)
        {
            var user = await GetByIdAsync(userId);
            if (user == null)
                return false;

            user.IsActive = true;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeactivateAsync(int userId)
        {
            var user = await GetByIdAsync(userId);
            if (user == null)
                return false;

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsAdminAsync(int userId)
        {
            var user = await GetByIdAsync(userId);
            return user?.IsAdmin ?? false;
        }

        public async Task<bool> SetAdminAsync(int userId, bool isAdmin)
        {
            var user = await GetByIdAsync(userId);
            if (user == null)
                return false;

            user.IsAdmin = isAdmin;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<ServiceResult> SendEmailVerificationCodeAsync(int userId, string email)
        {
            var user = await GetByIdAsync(userId);
            if (user == null)
            {
                return new ServiceResult 
                { 
                    Succeeded = false,
                    Errors = new List<string> { "用户不存在" }
                };
            }

            // TODO: 实现发送验证码的逻辑
            // 这里应该调用邮件服务发送验证码
            var verificationCode = GenerateVerificationCode();
            
            // 存储验证码，设置过期时间
            // 可以使用分布式缓存或数据库存储
            
            return new ServiceResult { Succeeded = true };
        }

        public async Task<ServiceResult> VerifyEmailAsync(int userId, string code)
        {
            var user = await GetByIdAsync(userId);
            if (user == null)
            {
                return new ServiceResult 
                { 
                    Succeeded = false,
                    Errors = new List<string> { "用户不存在" }
                };
            }

            // TODO: 验证验证码
            // 从缓存或数据库中获取验证码并验证

            user.IsEmailConfirmed = true;
            await _userManager.UpdateAsync(user);

            return new ServiceResult { Succeeded = true };
        }

        public async Task<ServiceResult> SendPhoneVerificationCodeAsync(int userId, string phoneNumber)
        {
            var user = await GetByIdAsync(userId);
            if (user == null)
            {
                return new ServiceResult 
                { 
                    Succeeded = false,
                    Errors = new List<string> { "用户不存在" }
                };
            }

            // TODO: 实现发送短信验证码的逻辑
            // 这里应该调用短信服务发送验证码
            var verificationCode = GenerateVerificationCode();
            
            // 存储验证码，设置过期时间
            // 可以使用分布式缓存或数据库存储
            
            return new ServiceResult { Succeeded = true };
        }

        public async Task<ServiceResult> VerifyPhoneNumberAsync(int userId, string phoneNumber, string code)
        {
            var user = await GetByIdAsync(userId);
            if (user == null)
            {
                return new ServiceResult 
                { 
                    Succeeded = false,
                    Errors = new List<string> { "用户不存在" }
                };
            }

            // TODO: 验证验证码
            // 从缓存或数据库中获取验证码并验证

            user.PhoneNumber = phoneNumber;
            user.PhoneNumberConfirmed = true;
            await _userManager.UpdateAsync(user);

            return new ServiceResult { Succeeded = true };
        }

        public async Task<ServiceResult> ToggleTwoFactorAuthenticationAsync(int userId)
        {
            var user = await GetByIdAsync(userId);
            if (user == null)
            {
                return new ServiceResult 
                { 
                    Succeeded = false,
                    Errors = new List<string> { "用户不存在" }
                };
            }

            user.TwoFactorEnabled = !user.TwoFactorEnabled;
            var result = await _userManager.UpdateAsync(user);

            return new ServiceResult 
            { 
                Succeeded = result.Succeeded,
                Errors = result.Errors.Select(e => e.Description).ToList()
            };
        }

        public async Task<ServiceResult> FollowAsync(int userId, int targetUserId)
        {
            if (userId == targetUserId)
            {
                return new ServiceResult 
                { 
                    Succeeded = false,
                    Errors = new List<string> { "不能关注自己" }
                };
            }

            var user = await GetByIdAsync(userId);
            var targetUser = await GetByIdAsync(targetUserId);

            if (user == null || targetUser == null)
            {
                return new ServiceResult 
                { 
                    Succeeded = false,
                    Errors = new List<string> { "用户不存在" }
                };
            }

            if (await IsFollowingAsync(userId, targetUserId))
            {
                return new ServiceResult 
                { 
                    Succeeded = false,
                    Errors = new List<string> { "已经关注过该用户" }
                };
            }

            user.Following.Add(targetUser);
            await _userManager.UpdateAsync(user);

            return new ServiceResult { Succeeded = true };
        }

        public async Task<ServiceResult> UnfollowAsync(int userId, int targetUserId)
        {
            var user = await GetByIdAsync(userId);
            var targetUser = await GetByIdAsync(targetUserId);

            if (user == null || targetUser == null)
            {
                return new ServiceResult 
                { 
                    Succeeded = false,
                    Errors = new List<string> { "用户不存在" }
                };
            }

            if (!await IsFollowingAsync(userId, targetUserId))
            {
                return new ServiceResult 
                { 
                    Succeeded = false,
                    Errors = new List<string> { "未关注该用户" }
                };
            }

            user.Following.Remove(targetUser);
            await _userManager.UpdateAsync(user);

            return new ServiceResult { Succeeded = true };
        }

        public async Task<List<User>> GetFollowingAsync(int userId)
        {
            var user = await _userManager.Users
                .Include(u => u.Following)
                .FirstOrDefaultAsync(u => u.Id == userId);

            return user?.Following.ToList() ?? new List<User>();
        }

        public async Task<int> GetFollowerCountAsync(int userId)
        {
            var user = await _userManager.Users
                .Include(u => u.Followers)
                .FirstOrDefaultAsync(u => u.Id == userId);

            return user?.Followers.Count ?? 0;
        }

        private string GenerateVerificationCode()
        {
            // 生成6位数字验证码
            Random random = new Random();
            return random.Next(100000, 999999).ToString();
        }
    }
} 