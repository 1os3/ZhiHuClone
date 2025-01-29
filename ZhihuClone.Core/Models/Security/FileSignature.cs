using System;
using System.ComponentModel.DataAnnotations;

namespace ZhihuClone.Core.Models.Security;

public class FileSignature
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Signature { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string FileType { get; set; } = null!;

    [Required]
    public string Status { get; set; } = "Active";

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsEnabled { get; set; } = true;

    public bool IsBlocked { get; set; }

    public string? Reason { get; set; }

    public DateTime? BlockedAt { get; set; }

    public DateTime? UnblockedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public int CreatedByUserId { get; set; }
    public virtual User? CreatedByUser { get; set; }

    public int? UpdatedByUserId { get; set; }
    public virtual User? UpdatedByUser { get; set; }

    // 文件相关属性
    public long MaxFileSize { get; set; } = 10485760; // 默认10MB
    public string? MimeType { get; set; }
    public string? FileExtension { get; set; }
    public int RiskLevel { get; set; } // 0-低风险, 1-中风险, 2-高风险
    public bool RequiresDeepScan { get; set; }
    public string? ValidationRules { get; set; } // JSON格式的验证规则
    public int DetectionCount { get; set; } // 检测到的次数
    public DateTime? LastDetectedAt { get; set; }

    public bool IsWhitelisted { get; set; }

    public int UseCount { get; set; }
    public DateTime? LastUsedAt { get; set; }

    public string Hash { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string Url { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public int UsageCount { get; set; }
} 