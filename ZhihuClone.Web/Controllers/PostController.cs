using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ZhihuClone.Core.Interfaces;
using ZhihuClone.Core.Models;
using ZhihuClone.Core.Models.Content;
using ZhihuClone.Web.Models.Post;

namespace ZhihuClone.Web.Controllers
{
    public class PostController : Controller
    {
        private readonly IPostService _postService;
        private readonly IMediaService _mediaService;
        private readonly IUserService _userService;
        private readonly ITopicService _topicService;

        public PostController(
            IPostService postService,
            IMediaService mediaService,
            IUserService userService,
            ITopicService topicService)
        {
            _postService = postService;
            _mediaService = mediaService;
            _userService = userService;
            _topicService = topicService;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            var pageSize = 20;
            var posts = await _postService.GetPagedAsync(page, pageSize);
            return View(posts);
        }

        [HttpGet("post/{id}")]
        public async Task<IActionResult> Detail(int id)
        {
            var post = await _postService.GetByIdAsync(id);
            if (post == null)
                return NotFound();

            var author = await _userService.GetByIdAsync(post.AuthorId);
            var topics = await _topicService.GetByPostIdAsync(id);

            ViewBag.Author = author;
            ViewBag.Topics = topics;

            // 增加浏览次数
            await _postService.IncrementViewCountAsync(id);

            return View(post);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Like(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0)
                return RedirectToAction("Login", "Account");

            await _postService.LikePostAsync(id, userId);
            return RedirectToAction(nameof(Detail), new { id });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Unlike(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0)
                return RedirectToAction("Login", "Account");

            await _postService.UnlikePostAsync(id, userId);
            return RedirectToAction(nameof(Detail), new { id });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Report(int id, string reason)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0)
                return RedirectToAction("Login", "Account");

            await _postService.ReportPostAsync(id, userId, reason, string.Empty);
            TempData["Message"] = "举报已提交，我们会尽快处理。";
            return RedirectToAction(nameof(Detail), new { id });
        }

        public async Task<IActionResult> Search(string keyword, int page = 1)
        {
            var pageSize = 20;
            var posts = await _postService.SearchPostsAsync(keyword, page, pageSize);
            ViewBag.Keyword = keyword;
            return View("Index", posts);
        }
    }
} 