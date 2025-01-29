using System.Collections.Generic;
using System.Threading.Tasks;
using ZhihuClone.Core.Models.Content;

namespace ZhihuClone.Core.Interfaces
{
    public interface ICommentService
    {
        Task<Comment?> GetByIdAsync(int id);
        Task<List<Comment>> GetAllAsync();
        Task<List<Comment>> GetPagedAsync(int page = 1, int pageSize = 20);
        Task<List<Comment>> GetByPostIdAsync(int postId);
        Task<List<Comment>> GetByUserIdAsync(int userId);
        Task<List<Comment>> GetRepliesAsync(int commentId);
        Task<Comment> CreateAsync(Comment comment);
        Task UpdateAsync(Comment comment);
        Task DeleteAsync(int id);
        Task<bool> LikeAsync(int commentId, int userId);
        Task<bool> UnlikeAsync(int commentId, int userId);
        Task<bool> IsLikedByUserAsync(int commentId, int userId);
        Task<CommentReport> ReportAsync(int commentId, int reporterId, string reason, string description);
        Task<List<CommentReport>> GetReportsAsync(int page = 1, int pageSize = 20);
        Task<List<CommentReport>> GetUnprocessedReportsAsync(int page = 1, int pageSize = 20);
        Task ProcessReportAsync(int reportId, int processedByUserId, string processResult);
        Task<Comment?> GetCommentByIdAsync(int id);
        Task<List<Comment>> GetCommentsByPostIdAsync(int postId, int page = 1, int pageSize = 20);
        Task<List<Comment>> GetRepliesByCommentIdAsync(int commentId, int page = 1, int pageSize = 20);
        Task<Comment> CreateCommentAsync(Comment comment);
        Task<Comment> UpdateCommentAsync(Comment comment);
        Task<bool> DeleteCommentAsync(int id);
        Task<bool> LikeCommentAsync(int commentId, int userId);
        Task<bool> UnlikeCommentAsync(int commentId, int userId);
        Task<bool> IsCommentLikedByUserAsync(int commentId, string? username);
        Task<int> GetCommentCountAsync(int postId);
        Task<bool> ReportCommentAsync(int commentId, int userId, string reason, string description);
        Task<IEnumerable<Comment>> GetCommentsByUserIdAsync(int userId);
        Task<int> GetLikesCountAsync(int commentId);
        Task<(List<Comment> Comments, int TotalCount)> GetPagedCommentsByPostAsync(int postId, int page, int pageSize);
        Task<List<Comment>> GetTopCommentsAsync(int postId, int count = 5);
        Task<List<Comment>> GetRecentCommentsAsync(int postId, int count = 5);
    }
} 