using System;
using ZhihuClone.Core.Models.Content;

namespace ZhihuClone.Core.Models
{
    public class PostReport
    {
        public int Id { get; set; }
        public int PostId { get; set; }
        public Post? Post { get; set; }
        public int ReporterId { get; set; }
        public User? Reporter { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsProcessed { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string? ProcessResult { get; set; }
        public int? ProcessedByUserId { get; set; }
        public User? ProcessedByUser { get; set; }
        public DateTime CreatedAt { get; set; }
    }
} 