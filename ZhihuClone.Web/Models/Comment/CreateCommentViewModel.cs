using System.ComponentModel.DataAnnotations;

namespace ZhihuClone.Web.Models.Comment
{
    public class CreateCommentViewModel
    {
        [Required]
        public int PostId { get; set; }

        [Required]
        [StringLength(1000)]
        public string Content { get; set; } = string.Empty;

        public int? ParentId { get; set; }

        public bool IsAnonymous { get; set; }
    }
} 