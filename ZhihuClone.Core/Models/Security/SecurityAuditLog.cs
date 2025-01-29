using System;
using System.ComponentModel.DataAnnotations;

namespace ZhihuClone.Core.Models.Security
{
    public class SecurityAuditLog
    {
        public int Id { get; set; }
        
        [Required]
        public string Action { get; set; } = null!;  // 操作类型：创建/修改/删除/审核等
        
        [Required]
        public string EntityType { get; set; } = null!;  // 实体类型：规则/配置/权限等
        
        public string? EntityId { get; set; }
        public string? OldValue { get; set; }  // 修改前的值（JSON格式）
        public string? NewValue { get; set; }  // 修改后的值（JSON格式）
        
        [Required]
        public string OperatorId { get; set; } = null!;  // 操作者ID
        public string? OperatorName { get; set; }
        public string? OperatorRole { get; set; }
        
        public string? IpAddress { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsAutomated { get; set; }  // 是否自动操作
        public string? Reason { get; set; }    // 操作原因
        public bool RequiresApproval { get; set; }
        public bool IsApproved { get; set; }
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        
        // 用于变更追踪
        public string? BatchId { get; set; }   // 批量操作ID
        public int ChangeVersion { get; set; } // 变更版本
        public bool IsReverted { get; set; }   // 是否已回滚
        public DateTime? RevertedAt { get; set; }
        public string? RevertedBy { get; set; }
    }
} 