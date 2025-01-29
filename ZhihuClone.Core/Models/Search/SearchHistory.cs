using System;
using System.ComponentModel.DataAnnotations;

namespace ZhihuClone.Core.Models.Search
{
    public class SearchHistory
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Keyword { get; set; } = null!;
        
        public int SearchCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastSearchedAt { get; set; }

        public DateTime SearchTime { get; set; }
        public int ResultCount { get; set; }
        public double Duration { get; set; }

        // 导航属性
        public virtual User User { get; set; } = null!;
    }
} 