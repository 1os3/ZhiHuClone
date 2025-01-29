using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using ZhihuClone.Core.Models.Content;

namespace ZhihuClone.Web.Models.Post
{
    public class CreatePostViewModel
    {
        [Required(ErrorMessage = "标题不能为空")]
        [StringLength(200, ErrorMessage = "标题长度不能超过200个字符")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "内容不能为空")]
        public string Content { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "摘要长度不能超过500个字符")]
        public string Summary { get; set; } = string.Empty;

        public IFormFile? CoverImage { get; set; }

        public bool IsAnonymous { get; set; }

        public List<int> TopicIds { get; set; } = new();

        public List<IFormFile> MediaFiles { get; set; } = new();
    }
} 