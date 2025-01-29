using System;
using System.Collections.Generic;
using ZhihuClone.Web.Models.User;

namespace ZhihuClone.Web.Models.Search
{
    public class SearchResult
    {
        public string Url { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string? Excerpt { get; set; }
        public UserViewModel Author { get; set; } = null!;
        public string Type { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public List<string> Tags { get; set; } = new();
        public double Score { get; set; }
    }
} 