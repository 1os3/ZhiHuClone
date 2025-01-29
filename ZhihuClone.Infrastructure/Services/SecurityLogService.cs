using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZhihuClone.Core.Interfaces;
using ZhihuClone.Core.Models.Security;
using ZhihuClone.Infrastructure.Data;

namespace ZhihuClone.Infrastructure.Services
{
    public class SecurityLogService : ISecurityLogService
    {
        private readonly ApplicationDbContext _context;

        public SecurityLogService(ApplicationDbContext context)
        {
            _context = context;
        }

        // 基础日志操作
        public async Task<SecurityLog> CreateLogAsync(SecurityLog log)
        {
            log.CreatedAt = DateTime.UtcNow;
            _context.SecurityLogs.Add(log);
            await _context.SaveChangesAsync();
            return log;
        }

        public async Task<SecurityLog?> GetLogByIdAsync(int id)
        {
            return await _context.SecurityLogs.FindAsync(id);
        }

        public async Task<IEnumerable<SecurityLog>> GetLogsByFilterAsync(
            DateTime? startTime = null,
            DateTime? endTime = null,
            string? eventType = null,
            string? level = null,
            string? ipAddress = null,
            string? userId = null,
            bool? requiresAction = null,
            bool? isResolved = null,
            int? severity = null,
            string? category = null)
        {
            var query = _context.SecurityLogs.AsQueryable();

            if (startTime.HasValue)
                query = query.Where(l => l.CreatedAt >= startTime.Value);
            if (endTime.HasValue)
                query = query.Where(l => l.CreatedAt <= endTime.Value);
            if (!string.IsNullOrEmpty(eventType))
                query = query.Where(l => l.EventType == eventType);
            if (!string.IsNullOrEmpty(level))
                query = query.Where(l => l.Level == level);
            if (!string.IsNullOrEmpty(ipAddress))
                query = query.Where(l => l.IpAddress == ipAddress);
            if (!string.IsNullOrEmpty(userId))
                query = query.Where(l => l.UserId == userId);
            if (requiresAction.HasValue)
                query = query.Where(l => l.RequiresAction == requiresAction.Value);
            if (isResolved.HasValue)
                query = query.Where(l => l.IsResolved == isResolved.Value);
            if (severity.HasValue)
                query = query.Where(l => l.Severity == severity.Value);
            if (!string.IsNullOrEmpty(category))
                query = query.Where(l => l.Category == category);

            return await query.OrderByDescending(l => l.CreatedAt).ToListAsync();
        }

        // 日志分析
        public async Task<IEnumerable<SecurityLog>> GetCorrelatedEventsAsync(string correlationId)
        {
            return await _context.SecurityLogs
                .Where(l => l.CorrelationId == correlationId)
                .OrderBy(l => l.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<SecurityLog>> GetSessionEventsAsync(string sessionId)
        {
            return await _context.SecurityLogs
                .Where(l => l.SessionId == sessionId)
                .OrderBy(l => l.CreatedAt)
                .ToListAsync();
        }

        public async Task<Dictionary<string, int>> GetEventTypeStatisticsAsync(DateTime startTime, DateTime endTime)
        {
            return await _context.SecurityLogs
                .Where(l => l.CreatedAt >= startTime && l.CreatedAt <= endTime)
                .GroupBy(l => l.EventType)
                .Select(g => new { EventType = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.EventType, x => x.Count);
        }

        public async Task<Dictionary<string, int>> GetSeverityStatisticsAsync(DateTime startTime, DateTime endTime)
        {
            return await _context.SecurityLogs
                .Where(l => l.CreatedAt >= startTime && l.CreatedAt <= endTime)
                .GroupBy(l => l.Severity)
                .Select(g => new { Severity = g.Key.ToString(), Count = g.Count() })
                .ToDictionaryAsync(x => x.Severity, x => x.Count);
        }

        // 日志处理
        public async Task<bool> MarkAsResolvedAsync(int logId, string resolution, string resolvedBy)
        {
            var log = await _context.SecurityLogs.FindAsync(logId);
            if (log == null) return false;

            log.IsResolved = true;
            log.Resolution = resolution;
            log.ResolvedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RequireActionAsync(int logId, string reason)
        {
            var log = await _context.SecurityLogs.FindAsync(logId);
            if (log == null) return false;

            log.RequiresAction = true;
            log.Details = reason;
            
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<SecurityLog>> GetUnresolvedHighSeverityLogsAsync()
        {
            return await _context.SecurityLogs
                .Where(l => !l.IsResolved && l.Severity >= 2)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();
        }

        // 日志清理
        public async Task<int> ArchiveOldLogsAsync(DateTime beforeDate)
        {
            // 这里应该实现归档逻辑，比如复制到归档表或导出到文件
            // 暂时只是标记为已归档
            var logsToArchive = await _context.SecurityLogs
                .Where(l => l.CreatedAt < beforeDate)
                .ToListAsync();

            foreach (var log in logsToArchive)
            {
                log.IsResolved = true;
                log.Resolution = "Archived automatically";
                log.ResolvedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return logsToArchive.Count;
        }

        public async Task<bool> DeleteLogAsync(int logId)
        {
            var log = await _context.SecurityLogs.FindAsync(logId);
            if (log == null) return false;

            _context.SecurityLogs.Remove(log);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> CleanupLogsAsync(DateTime beforeDate, string? category = null)
        {
            var query = _context.SecurityLogs.Where(l => l.CreatedAt < beforeDate);
            
            if (!string.IsNullOrEmpty(category))
                query = query.Where(l => l.Category == category);

            var logsToDelete = await query.ToListAsync();
            _context.SecurityLogs.RemoveRange(logsToDelete);
            await _context.SaveChangesAsync();
            
            return logsToDelete.Count;
        }
    }
} 