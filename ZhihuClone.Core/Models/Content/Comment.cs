using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ZhihuClone.Core.Models.Content
{
    public class Comment
    {
        public int Id { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        public int PostId { get; set; }
        public virtual Post Post { get; set; } = null!;

        public int AuthorId { get; set; }
        public virtual User Author { get; set; } = null!;

        public int? ParentId { get; set; }
        public virtual Comment? Parent { get; set; }

        public List<Comment> Replies { get; set; } = new();

        public List<Media> Media { get; set; } = new();

        public List<User> LikedUsers { get; set; } = new();

        public int LikeCount { get; set; }

        public bool IsAnonymous { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public int ReplyCount { get; set; }

        public bool IsDeleted { get; set; }

        public virtual ICollection<CommentReport> Reports { get; set; } = new List<CommentReport>();
    }
} 