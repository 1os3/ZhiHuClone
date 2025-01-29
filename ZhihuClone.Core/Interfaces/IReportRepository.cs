using System.Collections.Generic;
using System.Threading.Tasks;
using ZhihuClone.Core.Models;

namespace ZhihuClone.Core.Interfaces
{
    public interface IReportRepository : IRepository<Report>
    {
        Task<List<Report>> GetByUserIdAsync(int userId, int page = 1, int pageSize = 10);
        Task<List<Report>> GetByPostIdAsync(int postId);
        Task<List<Report>> GetByCommentIdAsync(int commentId);
        Task<List<Report>> GetByStatusAsync(string status, int page = 1, int pageSize = 10);
        Task<List<Report>> GetPendingReportsAsync(int page = 1, int pageSize = 10);
        Task<int> UpdateStatusAsync(int id, string status, string? note = null);
        Task<int> CountByStatusAsync(string status);
    }
} 