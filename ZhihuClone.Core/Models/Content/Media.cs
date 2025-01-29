using System.ComponentModel.DataAnnotations;

namespace ZhihuClone.Core.Models.Content
{
    public enum MediaType
    {
        Other = 0,
        Image = 1,
        Video = 2,
        Audio = 3
    }

    public class Media
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string ContentType { get; set; } = string.Empty;

        public long FileSize { get; set; }

        [Required]
        public string Url { get; set; } = string.Empty;

        public string? ThumbnailUrl { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; }

        public int CreatedByUserId { get; set; }

        public virtual User CreatedByUser { get; set; } = null!;

        public MediaType Type { get; set; }

        public string Extension { get; set; } = string.Empty;

        public string MimeType { get; set; } = string.Empty;

        public long Size { get; set; }

        public List<Post> Posts { get; set; } = new();

        public List<Comment> Comments { get; set; } = new();
    }
} 