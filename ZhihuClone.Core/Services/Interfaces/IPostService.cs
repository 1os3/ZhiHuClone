using System.Collections.Generic;
using System.Threading.Tasks;
using ZhihuClone.Core.Models.Content;

namespace ZhihuClone.Core.Services.Interfaces
{
    public interface IPostService
    {
        Task<Post?> GetByIdAsync(int id);
        Task<List<Post>> GetAllAsync();
        Task<List<Post>> GetPagedAsync(int page = 1, int pageSize = 10);
        Task<List<Post>> GetByUserIdAsync(int userId);
        Task<List<Post>> GetByTopicIdAsync(int topicId);
        Task<List<Post>> SearchAsync(string searchTerm, int page = 1, int pageSize = 10);
        Task<int> GetTotalCountAsync(string? searchTerm = null);
        Task CreateAsync(Post post);
        Task UpdateAsync(Post post);
        Task DeleteAsync(int id);
        Task LikeAsync(int userId, int postId);
        Task UnlikeAsync(int userId, int postId);
        Task<bool> IsLikedAsync(int userId, int postId);
        Task CollectAsync(int userId, int postId);
        Task UncollectAsync(int userId, int postId);
        Task<bool> IsCollectedAsync(int userId, int postId);
        Task<int> GetLikeCountAsync(int postId);
        Task<int> GetCollectCountAsync(int postId);
        Task<int> GetCommentCountAsync(int postId);
        Task<int> GetViewCountAsync(int postId);
        Task IncrementViewCountAsync(int postId);
    }
} 