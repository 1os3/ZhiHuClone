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
    public class SecurityAuditService : ISecurityAuditService
    {
        private readonly ApplicationDbContext _context;

        public SecurityAuditService(ApplicationDbContext context)
        {
            _context = context;
        }

        // 基础审计操作
        public async Task<SecurityAuditLog> CreateAuditLogAsync(SecurityAuditLog log)
        {
            log.Timestamp = DateTime.UtcNow;
            _context.SecurityAuditLogs.Add(log);
            await _context.SaveChangesAsync();
            return log;
        }

        public async Task<SecurityAuditLog?> GetAuditLogByIdAsync(int id)
        {
            return await _context.SecurityAuditLogs.FindAsync(id);
        }

        public async Task<IEnumerable<SecurityAuditLog>> GetAuditLogsByFilterAsync(
            DateTime? startTime = null,
            DateTime? endTime = null,
            string? action = null,
            string? entityType = null,
            string? operatorId = null,
            bool? isAutomated = null,
            bool? requiresApproval = null,
            bool? isApproved = null)
        {
            var query = _context.SecurityAuditLogs.AsQueryable();

            if (startTime.HasValue)
                query = query.Where(l => l.Timestamp >= startTime.Value);
            if (endTime.HasValue)
                query = query.Where(l => l.Timestamp <= endTime.Value);
            if (!string.IsNullOrEmpty(action))
                query = query.Where(l => l.Action == action);
            if (!string.IsNullOrEmpty(entityType))
                query = query.Where(l => l.EntityType == entityType);
            if (!string.IsNullOrEmpty(operatorId))
                query = query.Where(l => l.OperatorId == operatorId);
            if (isAutomated.HasValue)
                query = query.Where(l => l.IsAutomated == isAutomated.Value);
            if (requiresApproval.HasValue)
                query = query.Where(l => l.RequiresApproval == requiresApproval.Value);
            if (isApproved.HasValue)
                query = query.Where(l => l.IsApproved == isApproved.Value);

            return await query.OrderByDescending(l => l.Timestamp).ToListAsync();
        }

        // 变更追踪
        public async Task<IEnumerable<SecurityAuditLog>> GetBatchChangesAsync(string batchId)
        {
            return await _context.SecurityAuditLogs
                .Where(l => l.BatchId == batchId)
                .OrderBy(l => l.Timestamp)
                .ToListAsync();
        }

        public async Task<SecurityAuditLog?> GetLatestChangeAsync(string entityType, string entityId)
        {
            return await _context.SecurityAuditLogs
                .Where(l => l.EntityType == entityType && l.EntityId == entityId)
                .OrderByDescending(l => l.Timestamp)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<SecurityAuditLog>> GetChangeHistoryAsync(string entityType, string entityId)
        {
            return await _context.SecurityAuditLogs
                .Where(l => l.EntityType == entityType && l.EntityId == entityId)
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();
        }

        // 审批流程
        public async Task<bool> ApproveChangeAsync(int logId, string approvedBy, string? reason = null)
        {
            var log = await _context.SecurityAuditLogs.FindAsync(logId);
            if (log == null || !log.RequiresApproval || log.IsApproved) return false;

            log.IsApproved = true;
            log.ApprovedBy = approvedBy;
            log.ApprovedAt = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(reason))
                log.Reason = reason;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RejectChangeAsync(int logId, string rejectedBy, string reason)
        {
            var log = await _context.SecurityAuditLogs.FindAsync(logId);
            if (log == null || !log.RequiresApproval || log.IsApproved) return false;

            log.IsApproved = false;
            log.ApprovedBy = rejectedBy;
            log.ApprovedAt = DateTime.UtcNow;
            log.Reason = reason;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<SecurityAuditLog>> GetPendingApprovalsAsync(string? approverRole = null)
        {
            var query = _context.SecurityAuditLogs
                .Where(l => l.RequiresApproval && !l.IsApproved);

            if (!string.IsNullOrEmpty(approverRole))
                query = query.Where(l => l.OperatorRole == approverRole);

            return await query.OrderByDescending(l => l.Timestamp).ToListAsync();
        }

        // 变更回滚
        public async Task<bool> RevertChangeAsync(int logId, string revertedBy, string reason)
        {
            var log = await _context.SecurityAuditLogs.FindAsync(logId);
            if (log == null || log.IsReverted) return false;

            log.IsReverted = true;
            log.RevertedBy = revertedBy;
            log.RevertedAt = DateTime.UtcNow;

            // 创建一个新的审计日志记录回滚操作
            var revertLog = new SecurityAuditLog
            {
                Action = "Revert",
                EntityType = log.EntityType,
                EntityId = log.EntityId,
                OldValue = log.NewValue,
                NewValue = log.OldValue,
                OperatorId = revertedBy,
                Reason = reason,
                Timestamp = DateTime.UtcNow,
                BatchId = log.BatchId
            };

            _context.SecurityAuditLogs.Add(revertLog);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RevertBatchAsync(string batchId, string revertedBy, string reason)
        {
            var logs = await _context.SecurityAuditLogs
                .Where(l => l.BatchId == batchId && !l.IsReverted)
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();

            if (!logs.Any()) return false;

            foreach (var log in logs)
            {
                await RevertChangeAsync(log.Id, revertedBy, reason);
            }

            return true;
        }

        public async Task<IEnumerable<SecurityAuditLog>> GetRevertibleChangesAsync(DateTime since)
        {
            return await _context.SecurityAuditLogs
                .Where(l => l.Timestamp >= since && !l.IsReverted)
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();
        }

        // 统计分析
        public async Task<Dictionary<string, int>> GetActionStatisticsAsync(DateTime startTime, DateTime endTime)
        {
            return await _context.SecurityAuditLogs
                .Where(l => l.Timestamp >= startTime && l.Timestamp <= endTime)
                .GroupBy(l => l.Action)
                .Select(g => new { Action = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Action, x => x.Count);
        }

        public async Task<Dictionary<string, int>> GetOperatorStatisticsAsync(DateTime startTime, DateTime endTime)
        {
            return await _context.SecurityAuditLogs
                .Where(l => l.Timestamp >= startTime && l.Timestamp <= endTime)
                .GroupBy(l => l.OperatorId)
                .Select(g => new { OperatorId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.OperatorId, x => x.Count);
        }

        public async Task<IEnumerable<SecurityAuditLog>> GetSuspiciousActivitiesAsync()
        {
            // 这里实现可疑活动的检测逻辑，例如：
            // 1. 短时间内大量操作
            // 2. 非常规时间的操作
            // 3. 异常的操作模式
            var threshold = DateTime.UtcNow.AddMinutes(-5);
            
            var suspiciousLogs = await _context.SecurityAuditLogs
                .Where(l => l.Timestamp >= threshold)
                .GroupBy(l => new { l.OperatorId, l.Action })
                .Select(g => new { g.Key, Count = g.Count() })
                .Where(x => x.Count > 10) // 5分钟内同一操作超过10次
                .SelectMany(x => _context.SecurityAuditLogs
                    .Where(l => l.OperatorId == x.Key.OperatorId && 
                               l.Action == x.Key.Action &&
                               l.Timestamp >= threshold))
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();

            return suspiciousLogs;
        }
    }
} 