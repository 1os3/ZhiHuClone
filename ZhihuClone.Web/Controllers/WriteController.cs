using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZhihuClone.Core.Interfaces;
using ZhihuClone.Core.Models.Content;
using ZhihuClone.Web.Models.Post;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace ZhihuClone.Web.Controllers
{
    [Authorize]
    public class WriteController : Controller
    {
        private readonly IPostService _postService;
        private readonly IMediaService _mediaService;
        private readonly ITopicService _topicService;
        private readonly IUserService _userService;
        private readonly ILogger<WriteController> _logger;

        public WriteController(
            IPostService postService,
            IMediaService mediaService,
            ITopicService topicService,
            IUserService userService,
            ILogger<WriteController> logger)
        {
            _postService = postService;
            _mediaService = mediaService;
            _topicService = topicService;
            _userService = userService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var topics = await _topicService.GetAllAsync();
            ViewBag.Topics = topics;
            return View(new CreatePostViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreatePostViewModel model)
        {
            try
            {
                // 1. 基本验证
                if (!ModelState.IsValid)
                {
                    var topics = await _topicService.GetAllAsync();
                    ViewBag.Topics = topics;
                    return View("Index", model);
                }

                var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (userId == 0)
                {
                    _logger.LogWarning("用户ID无效");
                    return RedirectToAction("Login", "Account");
                }

                // 2. 验证用户是否存在且有效
                var user = await _userService.GetByIdAsync(userId);
                if (user == null || !user.IsActive)
                {
                    _logger.LogWarning($"用户 {userId} 不存在或已被禁用");
                    ModelState.AddModelError(string.Empty, "用户账号无效或已被禁用");
                    var topics = await _topicService.GetAllAsync();
                    ViewBag.Topics = topics;
                    return View("Index", model);
                }

                // 3. 处理文章内容
                var post = new Post
                {
                    Title = model.Title.Trim(),
                    Content = model.Content.Trim(),
                    Summary = string.IsNullOrEmpty(model.Summary) 
                        ? (model.Content.Length > 500 ? model.Content.Substring(0, 497) + "..." : model.Content)
                        : model.Summary.Trim(),
                    AuthorId = userId,
                    IsAnonymous = model.IsAnonymous,
                    Status = PostStatus.Published,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // 4. 处理封面图片
                if (model.CoverImage != null)
                {
                    try
                    {
                        var coverImagePath = await _mediaService.SaveFileAsync(model.CoverImage);
                        post.CoverImage = coverImagePath;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "处理封面图片时发生错误");
                        ModelState.AddModelError(string.Empty, "上传封面图片失败，请重试");
                        var topics = await _topicService.GetAllAsync();
                        ViewBag.Topics = topics;
                        return View("Index", model);
                    }
                }

                // 5. 创建文章
                var createdPost = await _postService.CreateAsync(post);
                if (createdPost == null)
                {
                    _logger.LogError("创建文章失败");
                    ModelState.AddModelError(string.Empty, "创建文章失败，请重试");
                    var topics = await _topicService.GetAllAsync();
                    ViewBag.Topics = topics;
                    return View("Index", model);
                }

                // 6. 处理话题关联
                if (model.TopicIds != null && model.TopicIds.Any())
                {
                    var validTopics = await _topicService.GetByIdsAsync(model.TopicIds);
                    foreach (var topicId in validTopics.Select(t => t.Id))
                    {
                        var result = await _postService.AddTopicAsync(createdPost.Id, topicId);
                        if (!result.Succeeded)
                        {
                            _logger.LogWarning($"添加话题 {topicId} 失败: {string.Join(", ", result.Errors)}");
                        }
                    }
                }

                // 7. 处理媒体文件
                if (model.MediaFiles != null && model.MediaFiles.Any())
                {
                    foreach (var mediaFile in model.MediaFiles)
                    {
                        try
                        {
                            var media = await _mediaService.UploadMediaAsync(mediaFile, userId);
                            await _mediaService.AssociateMediaWithPostAsync(media.Id, createdPost.Id);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"处理媒体文件 {mediaFile.FileName} 时发生错误");
                        }
                    }
                }

                TempData["SuccessMessage"] = "文章发布成功！";
                return RedirectToAction("Detail", "Post", new { id = createdPost.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发布文章时发生错误");
                ModelState.AddModelError(string.Empty, "发布文章时发生错误，请重试");
                var topics = await _topicService.GetAllAsync();
                ViewBag.Topics = topics;
                return View("Index", model);
            }
        }

        [HttpGet("draft")]
        public async Task<IActionResult> Draft()
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0)
                return RedirectToAction("Login", "Account");

            var drafts = await _postService.GetUserDraftsAsync(userId);
            return View(drafts);
        }

        [HttpPost("draft")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveDraft([FromBody] CreatePostViewModel model)
        {
            if (string.IsNullOrEmpty(model.Title) && string.IsNullOrEmpty(model.Content))
                return BadRequest(new { error = "标题和内容不能都为空" });

            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0)
                return RedirectToAction("Login", "Account");

            var post = new Post
            {
                Title = !string.IsNullOrEmpty(model.Title) ? model.Title : "无标题草稿",
                Content = !string.IsNullOrEmpty(model.Content) ? model.Content : string.Empty,
                Summary = !string.IsNullOrEmpty(model.Summary) 
                    ? model.Summary 
                    : (!string.IsNullOrEmpty(model.Content) 
                        ? model.Content.Substring(0, Math.Min(200, model.Content.Length)) 
                        : string.Empty),
                AuthorId = userId,
                IsAnonymous = model.IsAnonymous,
                Status = PostStatus.Draft,
                CreatedAt = DateTime.UtcNow
            };

            // 保存草稿
            await _postService.CreateAsync(post);

            // 添加话题关联
            if (model.TopicIds != null && model.TopicIds.Any())
            {
                foreach (var topicId in model.TopicIds)
                {
                    await _postService.AddTopicAsync(post.Id, topicId);
                }
            }

            return Ok(new { id = post.Id });
        }

        [HttpGet("draft/{id}")]
        public async Task<IActionResult> EditDraft(int id)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0)
                return RedirectToAction("Login", "Account");

            var post = await _postService.GetByIdAsync(id);
            if (post == null || post.AuthorId != userId || post.Status != PostStatus.Draft)
                return NotFound();

            var topics = await _topicService.GetAllAsync();
            ViewBag.Topics = topics;

            var model = new CreatePostViewModel
            {
                Title = post.Title,
                Content = post.Content,
                Summary = post.Summary,
                IsAnonymous = post.IsAnonymous
            };

            return View("Index", model);
        }
    }
} 