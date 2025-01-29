using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZhihuClone.Core.Models.Security;

namespace ZhihuClone.Core.Interfaces.Security
{
    public interface IAccessLogRepository : IRepository<AccessLog>
    {
        Task<List<AccessLog>> GetByActionAsync(string action);
        Task<List<AccessLog>> GetByIpAsync(string ipAddress);
        Task<List<AccessLog>> GetByUserIdAsync(int userId);
        Task<List<AccessLog>> GetFailedAttemptsAsync(DateTime startTime, DateTime endTime);
        Task<int> CountAsync(string? action = null, string? ipAddress = null, int? userId = null, bool? isSuccess = null);
        Task<List<AccessLog>> GetPagedAsync(int page, int pageSize, string? action = null, string? ipAddress = null, int? userId = null, bool? isSuccess = null);
        Task<Dictionary<string, int>> GetActionStatisticsAsync(DateTime startTime, DateTime endTime);
        Task<Dictionary<string, int>> GetIpStatisticsAsync(DateTime startTime, DateTime endTime);
        Task<Dictionary<string, int>> GetUserStatisticsAsync(DateTime startTime, DateTime endTime);
    }
} 