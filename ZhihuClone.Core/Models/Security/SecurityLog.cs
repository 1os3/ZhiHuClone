using System;
using System.ComponentModel.DataAnnotations;

namespace ZhihuClone.Core.Models.Security
{
    public class SecurityLog
    {
        public int Id { get; set; }
        
        [Required]
        public string EventType { get; set; } = null!;  // 事件类型：文件检测/IP封禁/垃圾内容等
        
        [Required]
        public string Level { get; set; } = null!;  // 日志级别：Info/Warning/Error/Critical
        
        public string? IpAddress { get; set; }
        public string? UserId { get; set; }
        public string? UserAgent { get; set; }
        public string? RequestPath { get; set; }
        public string? RequestMethod { get; set; }
        
        [Required]
        public string Message { get; set; } = null!;
        public string? Details { get; set; }  // 详细信息（JSON格式）
        
        public bool RequiresAction { get; set; }
        public bool IsResolved { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string? Resolution { get; set; }
        
        public string? RelatedEntityType { get; set; }  // 关联实体类型（如Post/Comment/User等）
        public string? RelatedEntityId { get; set; }    // 关联实体ID
        
        public DateTime CreatedAt { get; set; }
        public string? Source { get; set; }  // 来源模块
        public int Severity { get; set; }    // 严重程度：0-低，1-中，2-高
        public string? Category { get; set; } // 分类：安全/性能/业务/系统等
        
        // 用于事件关联分析
        public string? CorrelationId { get; set; }
        public string? SessionId { get; set; }
        public string? TraceId { get; set; }
    }
} 