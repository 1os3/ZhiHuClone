using System;
using System.ComponentModel.DataAnnotations;

namespace ZhihuClone.Core.Models.Security;

public class SensitiveWord
{
    public int Id { get; set; }

    [Required]
    public string Word { get; set; } = string.Empty;

    [Required]
    public string Category { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsEnabled { get; set; } = true;

    public bool IsDeleted { get; set; }

    public bool IsRegex { get; set; }

    public int Level { get; set; } = 1;

    public int MatchCount { get; set; }

    public DateTime? LastMatchAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public int CreatedByUserId { get; set; }
    public virtual User CreatedByUser { get; set; } = null!;

    public int? UpdatedByUserId { get; set; }
    public virtual User? UpdatedByUser { get; set; }
} 