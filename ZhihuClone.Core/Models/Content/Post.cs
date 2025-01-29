using System.ComponentModel.DataAnnotations;

namespace ZhihuClone.Core.Models.Content
{
    public class Post
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = null!;

        [Required]
        public string Content { get; set; } = null!;

        [StringLength(500)]
        public string? Summary { get; set; }

        public string? CoverImage { get; set; }

        public int ViewCount { get; set; }

        public int LikeCount { get; set; }

        public int CommentCount { get; set; }

        public int CollectCount { get; set; }

        public int ShareCount { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public PostStatus Status { get; set; } = PostStatus.Draft;

        public int AuthorId { get; set; }
        public virtual User Author { get; set; } = null!;

        public List<Comment> Comments { get; set; } = new();

        public List<Topic> Topics { get; set; } = new();

        public List<Media> Media { get; set; } = new();

        public List<User> CollectedUsers { get; set; } = new();

        public List<User> LikedUsers { get; set; } = new();

        public bool IsAnonymous { get; set; }
    }
} 