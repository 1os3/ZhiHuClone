using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ZhihuClone.Core.Models.Content;

namespace ZhihuClone.Core.Models
{
    public class Collection
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }
        public string? Cover { get; set; }

        public int CreatedByUserId { get; set; }
        public User CreatedByUser { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public int ItemCount { get; set; }
        public int FollowerCount { get; set; }
        public int ViewCount { get; set; }

        public bool IsPublic { get; set; } = true;
        public bool IsPrivate { get; set; }
        public bool IsDeleted { get; set; }

        // Navigation properties
        public virtual ICollection<Post> Posts { get; set; } = new List<Post>();
        public virtual ICollection<User> Followers { get; set; } = new List<User>();

        // Foreign keys
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        
        public int PostId { get; set; }
        public Post Post { get; set; } = null!;

        // Computed properties
        public int PostCount => ItemCount;
    }
} 