using System;
using ZhihuClone.Core.Models;

namespace ZhihuClone.Core.Models.Security
{
    public class BlockedIp
    {
        public int Id { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public DateTime BlockedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UnblockedAt { get; set; }
        public bool IsEnabled { get; set; } = true;
        public int BlockedByUserId { get; set; }
        public virtual User BlockedByUser { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public virtual User CreatedByUser { get; set; } = null!;
        public int CreatedByUserId { get; set; }
        public int? UpdatedByUserId { get; set; }
        public virtual User? UpdatedByUser { get; set; }
        public string BlockType { get; set; } = "Manual";
    }
} 