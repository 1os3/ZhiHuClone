using System;
using System.ComponentModel.DataAnnotations;

namespace ZhihuClone.Core.Models.Security;

public class SpamPattern
{
    public int Id { get; set; }
    
    [Required]
    public string Pattern { get; set; } = string.Empty;
    
    [Required]
    public string Type { get; set; } = string.Empty;
    
    [Required]
    public string Category { get; set; } = string.Empty;
    
    public bool IsRegex { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool IsDeleted { get; set; }
    public string? Description { get; set; }
    public int MatchCount { get; set; }
    public DateTime? LastMatchAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? PatternType { get; set; }
    public bool IsActive { get; set; }
    public bool IsBlocked { get; set; }
    public int Priority { get; set; } = 1;
    public string? Replacement { get; set; }

    // 新增属性
    public int Severity { get; set; } // 0-低, 1-中, 2-高
    public bool RequiresManualReview { get; set; }
    public int FalsePositiveCount { get; set; }
    public double Accuracy { get; set; } // 准确率
    public string? Context { get; set; } // 适用上下文
    public string? Language { get; set; } // 适用语言
    public bool IsAutoUpdated { get; set; } // 是否自动更新
    public string? Source { get; set; } // 规则来源
    public string? ValidationLogic { get; set; } // JSON格式的验证逻辑

    public int CreatedByUserId { get; set; }
    public virtual User? CreatedByUser { get; set; }

    public int? UpdatedByUserId { get; set; }
    public virtual User? UpdatedByUser { get; set; }
} 