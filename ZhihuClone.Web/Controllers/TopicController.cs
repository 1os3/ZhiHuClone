using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ZhihuClone.Core.Interfaces;
using ZhihuClone.Web.Models.Topic;

namespace ZhihuClone.Web.Controllers
{
    [Route("[controller]")]
    public class TopicController : Controller
    {
        private readonly ITopicService _topicService;
        private readonly IPostService _postService;
        private readonly IUserService _userService;

        public TopicController(
            ITopicService topicService,
            IPostService postService,
            IUserService userService)
        {
            _topicService = topicService;
            _postService = postService;
            _userService = userService;
        }

        [HttpGet]
        public async Task<IActionResult> List()
        {
            var topics = await _topicService.GetAllAsync();
            return View(topics);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Index(int id)
        {
            var topic = await _topicService.GetByIdAsync(id);
            if (topic == null)
                return NotFound();

            var viewModel = new TopicViewModel
            {
                Id = topic.Id,
                Name = topic.Name,
                Description = topic.Description,
                IconUrl = topic.IconUrl,
                PostCount = topic.Posts.Count,
                FollowerCount = topic.Followers.Count,
                CreatedAt = topic.CreatedAt,
                UpdatedAt = topic.UpdatedAt
            };

            // 如果用户已登录，检查是否关注了该话题
            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = int.Parse(User.FindFirst("nameid")?.Value ?? "0");
                viewModel.IsFollowing = await _topicService.IsFollowingAsync(userId, id);
            }

            // 获取该话题下的文章
            var posts = await _postService.GetByTopicIdAsync(id);
            ViewBag.Posts = posts;

            return View(viewModel);
        }
    }
} 