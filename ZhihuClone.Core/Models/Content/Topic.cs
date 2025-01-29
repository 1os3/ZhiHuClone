using System.ComponentModel.DataAnnotations;

namespace ZhihuClone.Core.Models.Content
{
    public class Topic
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = null!;

        [StringLength(500)]
        public string? Description { get; set; }

        public string? IconUrl { get; set; }

        public int ParentId { get; set; }

        public Topic? Parent { get; set; }

        public List<Topic> Children { get; set; } = new();

        public List<Post> Posts { get; set; } = new();

        public List<User> Followers { get; set; } = new();

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; }

        public int CreatedByUserId { get; set; }

        public User? CreatedByUser { get; set; }
    }
} 