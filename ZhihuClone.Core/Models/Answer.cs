using System;
using System.Collections.Generic;
using ZhihuClone.Core.Models.Content;

namespace ZhihuClone.Core.Models
{
    public class Answer
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public int QuestionId { get; set; }
        public int AuthorId { get; set; }
        public int ViewCount { get; set; }
        public int LikeCount { get; set; }
        public int CommentCount { get; set; }
        public int CollectCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsAccepted { get; set; }

        public virtual Question Question { get; set; } = null!;
        public virtual User Author { get; set; } = null!;
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public virtual ICollection<User> LikedUsers { get; set; } = new List<User>();
        public virtual ICollection<User> CollectedUsers { get; set; } = new List<User>();
        public virtual ICollection<Media> Media { get; set; } = new List<Media>();
    }
} 