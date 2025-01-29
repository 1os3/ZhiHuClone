using System;
using ZhihuClone.Core.Models.Content;

namespace ZhihuClone.Core.Models
{
    public class PostLike
    {
        public int UserId { get; set; }
        public int PostId { get; set; }
        public DateTime CreatedAt { get; set; }

        public virtual User User { get; set; } = null!;
        public virtual Post Post { get; set; } = null!;

        // Shadow properties for EF Core
        public int PostId1 { get; set; }
        public int UserId1 { get; set; }
    }
} 