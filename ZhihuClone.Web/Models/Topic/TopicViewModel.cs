using System.ComponentModel.DataAnnotations;

namespace ZhihuClone.Web.Models.Topic
{
    public class TopicViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = null!;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(200)]
        public string? IconUrl { get; set; }

        public int PostCount { get; set; }
        public int FollowerCount { get; set; }
        public bool IsFollowing { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
} 