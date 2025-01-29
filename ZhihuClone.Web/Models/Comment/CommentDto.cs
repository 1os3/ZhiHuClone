using System;
using System.Collections.Generic;

namespace ZhihuClone.Web.Models.Comment
{
    public class CommentDto
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsAnonymous { get; set; }
        public int LikeCount { get; set; }
        public int ReplyCount { get; set; }
        
        public CommentAuthorDto Author { get; set; } = new();
        public List<CommentDto> Replies { get; set; } = new();
        public bool IsLiked { get; set; }
    }

    public class CommentAuthorDto
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Nickname { get; set; } = string.Empty;
        public string Avatar { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
    }
} 