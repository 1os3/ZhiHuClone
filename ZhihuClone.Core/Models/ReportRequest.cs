using System.ComponentModel.DataAnnotations;

namespace ZhihuClone.Core.Models
{
    public class ReportRequest
    {
        public int Id { get; set; }
        public int ReportedId { get; set; }
        public string ReportType { get; set; } = string.Empty;
        public required string Reason { get; set; }
        public int ReporterId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public required string Description { get; set; }
    }
} 