using System.Collections.Generic;
using System.Threading.Tasks;
using ZhihuClone.Core.Models.Content;

namespace ZhihuClone.Core.Interfaces
{
    public interface ICommentReportRepository
    {
        Task<CommentReport?> GetByIdAsync(int id);
        Task<List<CommentReport>> GetAll();
        Task<CommentReport> AddAsync(CommentReport report);
        Task<CommentReport> UpdateAsync(CommentReport report);
        Task DeleteAsync(int id);
    }
} 