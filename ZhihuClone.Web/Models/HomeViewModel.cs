using ZhihuClone.Core.Models;
using ZhihuClone.Core.Models.Content;

namespace ZhihuClone.Web.Models
{
    public class HomeViewModel
    {
        public IEnumerable<ZhihuClone.Core.Models.Content.Post> Posts { get; set; } = new List<ZhihuClone.Core.Models.Content.Post>();
        public IEnumerable<ZhihuClone.Core.Models.Content.Topic> HotTopics { get; set; } = new List<ZhihuClone.Core.Models.Content.Topic>();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 10;
    }
} 