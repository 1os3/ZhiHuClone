using System;
using System.ComponentModel.DataAnnotations;
using ZhihuClone.Core.Models.Content;

namespace ZhihuClone.Core.Models
{
    public class Report
    {
        public int Id { get; set; }

        public int ReporterId { get; set; }
        public User Reporter { get; set; } = null!;

        public int? PostId { get; set; }
        public Post? Post { get; set; }

        public int? CommentId { get; set; }
        public Comment? Comment { get; set; }

        [Required]
        public string Reason { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; }

        public int? ProcessedById { get; set; }
        public User? ProcessedBy { get; set; }

        public string? ProcessingResult { get; set; }
        public string? ProcessingNote { get; set; }

        public bool IsResolved { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string? Resolution { get; set; }

        public int? ResolvedByUserId { get; set; }
        public User? ResolvedByUser { get; set; }

        public bool IsProcessed { get; set; }
        public string ProcessResult { get; set; } = string.Empty;

        public int? ProcessedByUserId { get; set; }
        public User? ProcessedByUser { get; set; }

        public bool IsDeleted { get; set; }
    }
} 