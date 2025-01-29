using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZhihuClone.Core.Models.Security;

namespace ZhihuClone.Core.Interfaces
{
    public interface ISecurityLogService
    {
        // 基础日志操作
        Task<SecurityLog> CreateLogAsync(SecurityLog log);
        Task<SecurityLog?> GetLogByIdAsync(int id);
        Task<IEnumerable<SecurityLog>> GetLogsByFilterAsync(
            DateTime? startTime = null,
            DateTime? endTime = null,
            string? eventType = null,
            string? level = null,
            string? ipAddress = null,
            string? userId = null,
            bool? requiresAction = null,
            bool? isResolved = null,
            int? severity = null,
            string? category = null);
            
        // 日志分析
        Task<IEnumerable<SecurityLog>> GetCorrelatedEventsAsync(string correlationId);
        Task<IEnumerable<SecurityLog>> GetSessionEventsAsync(string sessionId);
        Task<Dictionary<string, int>> GetEventTypeStatisticsAsync(DateTime startTime, DateTime endTime);
        Task<Dictionary<string, int>> GetSeverityStatisticsAsync(DateTime startTime, DateTime endTime);
        
        // 日志处理
        Task<bool> MarkAsResolvedAsync(int logId, string resolution, string resolvedBy);
        Task<bool> RequireActionAsync(int logId, string reason);
        Task<IEnumerable<SecurityLog>> GetUnresolvedHighSeverityLogsAsync();
        
        // 日志清理
        Task<int> ArchiveOldLogsAsync(DateTime beforeDate);
        Task<bool> DeleteLogAsync(int logId);
        Task<int> CleanupLogsAsync(DateTime beforeDate, string? category = null);
    }
} 