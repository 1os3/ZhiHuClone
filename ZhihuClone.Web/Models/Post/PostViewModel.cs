using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ZhihuClone.Core.Models;
using ZhihuClone.Core.Models.Content;
using ZhihuClone.Web.Models.User;

namespace ZhihuClone.Web.Models.Post
{
    public class PostViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = null!;

        [Required]
        public string Content { get; set; } = null!;

        [StringLength(500)]
        public string? Summary { get; set; }

        public int UserId { get; set; }
        public string UserName { get; set; } = null!;
        public string UserAvatar { get; set; } = null!;
        public string? UserTitle { get; set; }

        public List<string> Topics { get; set; } = new();
        public PostStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int ViewCount { get; set; }
        public int LikeCount { get; set; }
        public int CommentCount { get; set; }
        public bool IsLiked { get; set; }
        public List<string> MediaUrls { get; set; } = new();
        public List<CommentViewModel> Comments { get; set; } = new();
        public UserViewModel Author { get; set; } = null!;
    }

    public class CommentViewModel
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string? AuthorAvatar { get; set; }
        public DateTime CreatedAt { get; set; }
        public int LikeCount { get; set; }
        public bool IsLiked { get; set; }
        public bool IsAnonymous { get; set; }
        public int? ParentCommentId { get; set; }
        public List<CommentViewModel> Replies { get; set; } = new();
    }
} 