using System.Collections.Generic;
using System.Threading.Tasks;
using ZhihuClone.Core.Models;
using Microsoft.AspNetCore.Http;

namespace ZhihuClone.Core.Interfaces
{
    public class ServiceResult
    {
        public bool Succeeded { get; set; }
        public List<string> Errors { get; set; } = new();
        public object? Data { get; set; }
    }

    public interface IUserService
    {
        Task<bool> IsUsernameTakenAsync(string username);
        Task<User?> GetByIdAsync(int id);
        Task<User?> GetByUsernameAsync(string username);
        Task<User?> GetUserByUsernameAsync(string username);
        Task<ServiceResult> UpdateAsync(User user);
        Task<string> UpdateAvatarAsync(int userId, IFormFile avatar);
        Task<bool> UpdateUserProfileAsync(int userId, string bio, string avatar);
        Task<bool> IsAdminAsync(int userId);
        Task<bool> SetAdminAsync(int userId, bool isAdmin);
        Task<ServiceResult> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
        Task<ServiceResult> ValidatePasswordAsync(int userId, string password);
        Task<ServiceResult> DeleteAccountAsync(int userId);
        Task<ServiceResult> SendEmailVerificationCodeAsync(int userId, string email);
        Task<ServiceResult> VerifyEmailAsync(int userId, string code);
        Task<ServiceResult> SendPhoneVerificationCodeAsync(int userId, string phoneNumber);
        Task<ServiceResult> VerifyPhoneNumberAsync(int userId, string phoneNumber, string code);
        Task<ServiceResult> ToggleTwoFactorAuthenticationAsync(int userId);
        Task<bool> IsFollowingAsync(int userId, int targetUserId);
        Task<ServiceResult> FollowAsync(int userId, int targetUserId);
        Task<ServiceResult> UnfollowAsync(int userId, int targetUserId);
        Task<List<User>> GetFollowingAsync(int userId);
        Task<List<User>> GetFollowersAsync(int userId);
        Task<int> GetFollowerCountAsync(int userId);
        Task<int> GetFollowingCountAsync(int userId);
        Task<bool> IsEmailUniqueAsync(string email);
        Task<bool> IsUsernameUniqueAsync(string username);
        Task<User> CreateUserAsync(User user, string password);
        Task<bool> FollowUserAsync(int followerId, int followedId);
        Task<bool> UnfollowUserAsync(int followerId, int followedId);
        Task<List<User>> GetAllUsersAsync();
    }
} 