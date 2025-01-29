using System.Collections.Generic;
using System.Threading.Tasks;
using ZhihuClone.Core.Models.Content;

namespace ZhihuClone.Core.Interfaces
{
    public interface ITopicRepository : IRepository<Topic>
    {
        new Task<Topic?> GetByIdAsync(int id);
        new Task<List<Topic>> GetAllAsync();
        Task<List<Topic>> GetByUserIdAsync(int userId);
        Task<List<Topic>> GetByPostIdAsync(int postId);
        Task<List<Topic>> GetHotTopicsAsync(int count);
        Task<List<Topic>> GetChildrenAsync(int parentId);
        Task<List<Topic>> GetPagedAsync(int page = 1, int pageSize = 10);
        Task<List<Topic>> SearchAsync(string searchTerm, int page = 1, int pageSize = 10);
        Task<int> GetTotalCountAsync(string? searchTerm = null);
        new Task<Topic> AddAsync(Topic topic);
        Task<Topic> UpdateAsync(Topic topic);
        Task DeleteAsync(int id);
        Task<int> CountByUserAsync(int userId);
        Task<bool> IsFollowedAsync(int userId, int topicId);
        Task<int> GetFollowerCountAsync(int topicId);
    }
} 