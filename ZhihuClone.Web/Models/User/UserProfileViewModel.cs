using System;
using System.Collections.Generic;
using ZhihuClone.Core.Models;
using ZhihuClone.Core.Models.Content;
using ZhihuClone.Core.Models.Search;

namespace ZhihuClone.Web.Models.User
{
    public class UserProfileViewModel
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string? Nickname { get; set; }
        public string? Avatar { get; set; }
        public string? Bio { get; set; }
        public string? Location { get; set; }
        public string? Company { get; set; }
        public string? Title { get; set; }
        public string? Website { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? Occupation { get; set; }
        public DateTime JoinDate { get; set; }
        public bool IsCurrentUser { get; set; }
        public bool IsFollowing { get; set; }
        public int FollowingCount { get; set; }
        public int FollowersCount { get; set; }
        public int PostCount { get; set; }
        public IEnumerable<ZhihuClone.Core.Models.Content.Post> Posts { get; set; } = new List<ZhihuClone.Core.Models.Content.Post>();
        public IEnumerable<Answer> Answers { get; set; } = new List<Answer>();
        public IEnumerable<Collection> Collections { get; set; } = new List<Collection>();
        public IEnumerable<SearchHistory> SearchHistories { get; set; } = new List<SearchHistory>();
    }
} 