using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZhihuClone.Core.Models.Security;

namespace ZhihuClone.Core.Interfaces
{
    public interface ISecurityAuditService
    {
        // 基础审计操作
        Task<SecurityAuditLog> CreateAuditLogAsync(SecurityAuditLog log);
        Task<SecurityAuditLog?> GetAuditLogByIdAsync(int id);
        Task<IEnumerable<SecurityAuditLog>> GetAuditLogsByFilterAsync(
            DateTime? startTime = null,
            DateTime? endTime = null,
            string? action = null,
            string? entityType = null,
            string? operatorId = null,
            bool? isAutomated = null,
            bool? requiresApproval = null,
            bool? isApproved = null);
            
        // 变更追踪
        Task<IEnumerable<SecurityAuditLog>> GetBatchChangesAsync(string batchId);
        Task<SecurityAuditLog?> GetLatestChangeAsync(string entityType, string entityId);
        Task<IEnumerable<SecurityAuditLog>> GetChangeHistoryAsync(string entityType, string entityId);
        
        // 审批流程
        Task<bool> ApproveChangeAsync(int logId, string approvedBy, string? reason = null);
        Task<bool> RejectChangeAsync(int logId, string rejectedBy, string reason);
        Task<IEnumerable<SecurityAuditLog>> GetPendingApprovalsAsync(string? approverRole = null);
        
        // 变更回滚
        Task<bool> RevertChangeAsync(int logId, string revertedBy, string reason);
        Task<bool> RevertBatchAsync(string batchId, string revertedBy, string reason);
        Task<IEnumerable<SecurityAuditLog>> GetRevertibleChangesAsync(DateTime since);
        
        // 统计分析
        Task<Dictionary<string, int>> GetActionStatisticsAsync(DateTime startTime, DateTime endTime);
        Task<Dictionary<string, int>> GetOperatorStatisticsAsync(DateTime startTime, DateTime endTime);
        Task<IEnumerable<SecurityAuditLog>> GetSuspiciousActivitiesAsync();
    }
} 