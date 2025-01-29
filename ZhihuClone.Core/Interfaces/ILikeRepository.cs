using System.Collections.Generic;
using System.Threading.Tasks;
using ZhihuClone.Core.Models;

namespace ZhihuClone.Core.Interfaces
{
    public interface ILikeRepository : IRepository<Like>
    {
        Task<List<Like>> GetByUserIdAsync(int userId);
        Task<List<Like>> GetByPostIdAsync(int postId);
        Task<List<Like>> GetByCommentIdAsync(int commentId);
        Task<bool> ExistsAsync(int userId, int? postId, int? commentId);
        Task<int> CountByPostAsync(int postId);
        Task<int> CountByCommentAsync(int commentId);
    }
} 