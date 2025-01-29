using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using ZhihuClone.Core.Models.Content;

namespace ZhihuClone.Core.Models
{
    public class User : IdentityUser<int>
    {
        public string Nickname { get; set; } = string.Empty;
        public string Avatar { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;
        public bool IsDeleted { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? LastLoginAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // 隐私设置
        public bool ShowEmail { get; set; }
        public bool ShowPhone { get; set; }
        public bool ShowLocation { get; set; }
        public bool ShowCompany { get; set; }
        public bool AllowFollow { get; set; } = true;
        public bool AllowDirectMessage { get; set; } = true;
        public bool AllowNotification { get; set; } = true;

        // 通知设置
        public bool EmailNotification { get; set; } = true;
        public bool PushNotification { get; set; } = true;
        public bool LikeNotification { get; set; } = true;
        public bool CommentNotification { get; set; } = true;
        public bool FollowNotification { get; set; } = true;
        public bool SystemNotification { get; set; } = true;

        public List<Post> Posts { get; set; } = new();
        public List<Comment> Comments { get; set; } = new();
        public List<Post> LikedPosts { get; set; } = new();
        public List<Post> CollectedPosts { get; set; } = new();
        public List<Comment> LikedComments { get; set; } = new();
        public List<Topic> FollowedTopics { get; set; } = new();
        public List<User> Followers { get; set; } = new();
        public List<User> Following { get; set; } = new();

        public bool IsEmailConfirmed
        {
            get => EmailConfirmed;
            set => EmailConfirmed = value;
        }
    }
} 