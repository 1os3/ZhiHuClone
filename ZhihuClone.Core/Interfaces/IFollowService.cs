using ZhihuClone.Core.Models;

namespace ZhihuClone.Core.Interfaces
{
    public interface IFollowService
    {
        Task<bool> IsFollowingAsync(int followerId, int followedId);
        Task<int> GetFollowingCountAsync(int userId);
        Task<int> GetFollowersCountAsync(int userId);
        Task FollowAsync(int followerId, int followedId);
        Task UnfollowAsync(int followerId, int followedId);
        Task<List<User>> GetFollowingAsync(int userId, int page);
        Task<List<User>> GetFollowersAsync(int userId, int page);
    }
} 