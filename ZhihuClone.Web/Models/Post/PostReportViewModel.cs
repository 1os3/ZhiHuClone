using System.ComponentModel.DataAnnotations;

namespace ZhihuClone.Web.Models.Post
{
    public class PostReportViewModel
    {
        [Required]
        [StringLength(100)]
        public string Reason { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;
    }
} 