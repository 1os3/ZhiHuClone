using System.Collections.Generic;
using System.Threading.Tasks;
using ZhihuClone.Core.Models.Content;

namespace ZhihuClone.Core.Interfaces
{
    public interface IPostRepository
    {
        Task<Post?> GetByIdAsync(int id);
        Task<List<Post>> GetAllAsync();
        Task<List<Post>> GetPagedAsync(int page = 1, int pageSize = 10);
        Task<List<Post>> GetByUserIdAsync(int userId);
        Task<List<Post>> GetByTopicIdAsync(int topicId);
        Task<List<Post>> SearchAsync(string searchTerm, int page = 1, int pageSize = 10);
        Task<int> GetTotalCountAsync(string? searchTerm = null);
        Task<Post> AddAsync(Post post);
        Task<Post> UpdateAsync(Post post);
        Task<bool> DeleteAsync(int id);
        Task<int> CountByUserAsync(int userId);
        Task<int> CountByTopicAsync(int topicId);
        Task<List<Post>> GetUserPostsAsync(int userId, int page = 1, int pageSize = 10);
        Task<List<Post>> GetTrendingPostsAsync(int count = 10);
        Task<List<Post>> GetLatestPostsAsync(int count = 10);
        Task<List<Post>> GetUserCollectionsAsync(int userId, int page = 1, int pageSize = 10);
        Task<List<Post>> GetPostsByTopicAsync(int topicId, int page = 1, int pageSize = 10);
        Task<List<Post>> GetAllPostsAsync(int page = 1, int pageSize = 10);
    }
} 