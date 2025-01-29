using ZhihuClone.Core.Models.Content;

namespace ZhihuClone.Core.Interfaces
{
    public interface ITopicService
    {
        Task<Topic?> GetByIdAsync(int id);
        Task<List<Topic>> GetAllAsync();
        Task<List<Topic>> GetHotTopicsAsync(int count);
        Task<List<Topic>> GetByUserIdAsync(int userId);
        Task<List<Topic>> GetByPostIdAsync(int postId);
        Task<List<Topic>> GetByIdsAsync(IEnumerable<int> ids);
        Task<List<Topic>> SearchAsync(string searchTerm, int page = 1, int pageSize = 10);
        Task<int> GetTotalCountAsync(string? searchTerm = null);
        Task<Topic> CreateAsync(Topic topic);
        Task UpdateAsync(Topic topic);
        Task DeleteAsync(int id);
        Task<bool> FollowAsync(int userId, int topicId);
        Task<bool> UnfollowAsync(int userId, int topicId);
        Task<bool> IsFollowingAsync(int userId, int topicId);
        Task<int> GetFollowerCountAsync(int topicId);
    }
} 