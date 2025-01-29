using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ZhihuClone.Core.Interfaces;
using ZhihuClone.Core.Models;
using ZhihuClone.Core.Models.Content;
using ZhihuClone.Web.Models.Comment;

namespace ZhihuClone.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CommentController : ControllerBase
    {
        private readonly ICommentService _commentService;
        private readonly IUserService _userService;

        public CommentController(ICommentService commentService, IUserService userService)
        {
            _commentService = commentService;
            _userService = userService;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var comment = await _commentService.GetByIdAsync(id);
            if (comment == null)
                return NotFound();

            var dto = await MapToCommentDto(comment);
            return Ok(dto);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCommentViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return Unauthorized();

            var comment = new Comment
            {
                Content = model.Content,
                PostId = model.PostId,
                AuthorId = userId,
                ParentId = model.ParentId,
                IsAnonymous = model.IsAnonymous,
                CreatedAt = DateTime.UtcNow
            };

            await _commentService.CreateAsync(comment);
            var dto = await MapToCommentDto(comment);
            return Ok(dto);
        }

        [Authorize]
        [HttpPost("{id}/like")]
        public async Task<IActionResult> Like(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return Unauthorized();

            var success = await _commentService.LikeAsync(id, userId);
            return success ? Ok() : BadRequest();
        }

        [Authorize]
        [HttpPost("{id}/unlike")]
        public async Task<IActionResult> Unlike(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return Unauthorized();

            var success = await _commentService.UnlikeAsync(id, userId);
            return success ? Ok() : BadRequest();
        }

        [Authorize]
        [HttpPost("{id}/report")]
        public async Task<IActionResult> Report(int id, [FromBody] CommentReportViewModel model)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return Unauthorized();

            var report = await _commentService.ReportAsync(id, userId, model.Reason, model.Description);
            return Ok(report);
        }

        [HttpGet("post/{postId}")]
        public async Task<IActionResult> GetByPost(int postId)
        {
            var comments = await _commentService.GetByPostIdAsync(postId);
            var dtos = await Task.WhenAll(comments.Select(MapToCommentDto));
            return Ok(dtos);
        }

        [HttpGet("{id}/replies")]
        public async Task<IActionResult> GetReplies(int id)
        {
            var replies = await _commentService.GetRepliesAsync(id);
            var dtos = await Task.WhenAll(replies.Select(MapToCommentDto));
            return Ok(dtos);
        }

        [HttpGet("{id}/isLiked")]
        public async Task<IActionResult> IsLiked(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return Ok(false);

            var isLiked = await _commentService.IsLikedByUserAsync(id, userId);
            return Ok(isLiked);
        }

        private async Task<CommentDto> MapToCommentDto(Comment comment)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int.TryParse(currentUserId, out int userId);

            var author = await _userService.GetByIdAsync(comment.AuthorId);
            var isLiked = userId > 0 ? await _commentService.IsLikedByUserAsync(comment.Id, userId) : false;
            var replies = await _commentService.GetRepliesAsync(comment.Id);

            return new CommentDto
            {
                Id = comment.Id,
                Content = comment.Content,
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt,
                IsAnonymous = comment.IsAnonymous,
                LikeCount = comment.LikeCount,
                ReplyCount = comment.ReplyCount,
                IsLiked = isLiked,
                Author = new CommentAuthorDto
                {
                    Id = author.Id,
                    UserName = author.UserName ?? string.Empty,
                    Nickname = author.Nickname,
                    Avatar = author.Avatar,
                    Bio = author.Bio
                },
                Replies = (await Task.WhenAll(replies.Select(MapToCommentDto))).ToList()
            };
        }
    }
} 