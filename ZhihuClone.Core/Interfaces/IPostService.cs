#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using ZhihuClone.Core.Models;
using ZhihuClone.Core.Models.Content;

namespace ZhihuClone.Core.Interfaces
{
    public interface IPostService
    {
        Task<Post?> GetByIdAsync(int id);
        Task<List<Post>> GetAllAsync();
        Task<List<Post>> GetPagedAsync(int page = 1, int pageSize = 20);
        Task<List<Post>> GetByUserIdAsync(int userId);
        Task<List<Post>> GetByTopicIdAsync(int topicId);
        Task<List<Post>> SearchAsync(string searchTerm, int page = 1, int pageSize = 20);
        Task<int> GetTotalCountAsync(string? searchTerm = null);
        Task<Post> CreateAsync(Post post);
        Task<ServiceResult> UpdateAsync(Post post);
        Task DeleteAsync(int id);
        Task<bool> LikeAsync(int postId, int userId);
        Task<bool> UnlikeAsync(int postId, int userId);
        Task<bool> IsLikedByUserAsync(int postId, int userId);
        Task<List<Post>> GetLatestAsync(int count = 10);
        Task<List<Post>> GetTrendingAsync(int count = 10);
        Task<List<Post>> GetUserCollectionsAsync(int userId, int page = 1, int pageSize = 10);
        Task<List<Post>> GetPostsByTopicAsync(int topicId, int page = 1, int pageSize = 10);
        Task<List<Post>> GetAllPostsAsync(int page = 1, int pageSize = 10);
        Task<List<Post>> SearchPostsAsync(string keyword, int page = 1, int pageSize = 20);
        Task<PostReport> ReportPostAsync(int postId, int reporterId, string reason, string description);
        Task<List<PostReport>> GetPostReportsAsync(int page = 1, int pageSize = 10);
        Task<List<PostReport>> GetUnprocessedReportsAsync(int page = 1, int pageSize = 10);
        Task ProcessReportAsync(int reportId, int processedByUserId, string processResult);
        Task<int> GetPostCountAsync();
        Task<bool> IsPostLikedByUserAsync(int postId, string? username);
        Task IncrementViewCountAsync(int postId);
        Task<List<Post>> GetUserPostsAsync(int userId, int page = 1, int pageSize = 10);
        Task<List<Post>> GetTrendingPostsAsync(int count = 10);
        Task<bool> LikePostAsync(int postId, int userId);
        Task<bool> UnlikePostAsync(int postId, int userId);
        Task<int> GetUserPostCountAsync(int userId);
        Task<List<Post>> GetUserRecentPostsAsync(int userId, int count);
        Task<List<Post>> GetUserDraftsAsync(int userId);
        Task<ServiceResult> AddTopicAsync(int postId, int topicId);
        Task<ServiceResult> RemoveTopicAsync(int postId, int topicId);
    }
} 