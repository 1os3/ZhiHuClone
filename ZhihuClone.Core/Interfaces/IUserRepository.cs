using System.Collections.Generic;
using System.Threading.Tasks;
using ZhihuClone.Core.Models;

namespace ZhihuClone.Core.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(int id);
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByUsernameAsync(string username);
        Task<List<User>> GetFollowersAsync(int userId, int page = 1, int pageSize = 10);
        Task<List<User>> GetFollowingAsync(int userId, int page = 1, int pageSize = 10);
        Task<User> AddAsync(User user);
        Task<User> UpdateAsync(User user);
        Task DeleteAsync(int id);
        Task<bool> IsFollowingAsync(int followerId, int followeeId);
        Task<int> GetFollowerCountAsync(int userId);
        Task<int> GetFollowingCountAsync(int userId);
        Task<List<User>> SearchAsync(string searchTerm, int page = 1, int pageSize = 10);
    }
} 