using System;
using System.ComponentModel.DataAnnotations;

namespace ZhihuClone.Core.Models.Content
{
    public class CommentReport
    {
        public int Id { get; set; }

        public int CommentId { get; set; }
        public virtual Comment Comment { get; set; } = null!;

        public int ReporterId { get; set; }
        public virtual User Reporter { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string Reason { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        public bool IsProcessed { get; set; }

        public DateTime? ProcessedAt { get; set; }

        public string? ProcessResult { get; set; }

        public int? ProcessedByUserId { get; set; }
        public virtual User? ProcessedByUser { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
} 