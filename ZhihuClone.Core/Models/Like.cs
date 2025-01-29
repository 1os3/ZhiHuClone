using System;
using System.ComponentModel.DataAnnotations;
using ZhihuClone.Core.Models.Content;

namespace ZhihuClone.Core.Models
{
    public class Like
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public int PostId { get; set; }
        public Post Post { get; set; } = null!;

        public int? CommentId { get; set; }
        public Comment? Comment { get; set; }

        public DateTime CreatedAt { get; set; }
    }
} 