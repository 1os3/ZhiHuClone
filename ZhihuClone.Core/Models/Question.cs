using System;
using System.Collections.Generic;
using ZhihuClone.Core.Models.Content;

namespace ZhihuClone.Core.Models
{
    public class Question
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int AuthorId { get; set; }
        public int ViewCount { get; set; }
        public int FollowCount { get; set; }
        public int AnswerCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }

        public virtual User Author { get; set; } = null!;
        public virtual ICollection<Answer> Answers { get; set; } = new List<Answer>();
        public virtual ICollection<Topic> Topics { get; set; } = new List<Topic>();
        public virtual ICollection<User> Followers { get; set; } = new List<User>();
        public virtual ICollection<Media> Media { get; set; } = new List<Media>();
    }
} 