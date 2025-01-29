using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ZhihuClone.Core.Models;
using ZhihuClone.Core.Models.Content;

namespace ZhihuClone.Core.Entities
{
    public class Comment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        public int PostId { get; set; }
        public Post Post { get; set; } = null!;

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public int? ParentCommentId { get; set; }
        public Comment? ParentComment { get; set; }

        public ICollection<Comment> Replies { get; set; } = new List<Comment>();
        public ICollection<Like> Likes { get; set; } = new List<Like>();

        public CommentStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public int LikeCount { get; set; }
        public int ReplyCount { get; set; }
        public bool IsDeleted { get; set; }

        public int AuthorId { get; set; }
        public List<User> LikedUsers { get; set; } = new();
        public List<Media> Media { get; set; } = new();
    }

    public enum CommentStatus
    {
        Draft = 0,
        Published = 1,
        Hidden = 2,
        Deleted = 3
    }
} 