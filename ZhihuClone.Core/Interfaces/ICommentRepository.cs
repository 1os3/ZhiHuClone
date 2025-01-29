using System.Collections.Generic;
using System.Threading.Tasks;
using ZhihuClone.Core.Models.Content;

namespace ZhihuClone.Core.Interfaces
{
    public interface ICommentRepository : IRepository<Comment>
    {
        new Task<Comment?> GetByIdAsync(int id);
        Task<List<Comment>> GetByPostIdAsync(int postId);
        Task<List<Comment>> GetByUserIdAsync(int userId);
        Task<List<Comment>> GetRepliesAsync(int commentId);
        new Task<Comment> AddAsync(Comment comment);
        Task<Comment> UpdateAsync(Comment comment);
        Task DeleteAsync(int id);
        Task<int> CountByPostAsync(int postId);
        Task<int> CountByUserAsync(int userId);
        Task<List<Comment>> GetPagedAsync(int page = 1, int pageSize = 10);
        Task<List<Comment>> SearchAsync(string searchTerm, int page = 1, int pageSize = 10);
        Task<int> GetTotalCountAsync(string? searchTerm = null);
        Task<bool> IsLikedAsync(int userId, int commentId);
        Task<int> GetLikeCountAsync(int commentId);
        Task<int> GetReplyCountAsync(int commentId);
        Task<bool> IsLikedByUserAsync(int commentId, int userId);
        Task<List<Comment>> GetPagedByPostAsync(int postId, int page, int pageSize);
        Task<List<Comment>> GetTopCommentsByPostAsync(int postId, int count);
        Task<List<Comment>> GetRecentCommentsByPostAsync(int postId, int count);
    }
} 