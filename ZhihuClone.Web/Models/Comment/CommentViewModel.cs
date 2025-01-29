using System.ComponentModel.DataAnnotations;

namespace ZhihuClone.Web.Models.Comment
{
    public class CommentViewModel
    {
        public int Id { get; set; }
        public int PostId { get; set; }
        public int? ParentId { get; set; }

        [Required]
        public string Content { get; set; } = null!;

        public int UserId { get; set; }
        public string UserName { get; set; } = null!;
        public string UserAvatar { get; set; } = null!;

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int LikeCount { get; set; }
        public bool IsLiked { get; set; }

        public List<CommentViewModel> Replies { get; set; } = new();
    }
} 