using System;
using System.Collections.Generic;
using ZhihuClone.Core.Models;
using ZhihuClone.Core.Models.Content;

namespace ZhihuClone.Core.Models.Search
{
    public class SearchResult
    {
        public string Url { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string? Excerpt { get; set; }
        public User Author { get; set; } = null!;
        public string Type { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public List<string> Tags { get; set; } = new();
        public int Score { get; set; }
        
        // 分页相关属性
        public List<Post> Posts { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }

        public List<User> Users { get; set; } = new();
        public List<Topic> Topics { get; set; } = new();
        public List<Comment> Comments { get; set; } = new();

        public int TotalUsers { get; set; }
        public int TotalPosts { get; set; }
        public int TotalTopics { get; set; }
        public int TotalComments { get; set; }

        public string? SearchTerm { get; set; }
    }
} 