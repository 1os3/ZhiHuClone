using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ZhihuClone.Core.Interfaces;
using ZhihuClone.Core.Interfaces.Security;
using ZhihuClone.Core.Models;
using ZhihuClone.Core.Services.Interfaces;
using System.ComponentModel.DataAnnotations;
using ZhihuClone.Core.Models.Content;

namespace ZhihuClone.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CommentsController : ControllerBase
    {
        private readonly ICommentService _commentService;
        private readonly IFirewallService _firewallService;

        public CommentsController(
            ICommentService commentService,
            IFirewallService firewallService)
        {
            _commentService = commentService;
            _firewallService = firewallService;
        }

        [HttpGet("post/{postId}")]
        public async Task<IActionResult> GetCommentsByPost(int postId)
        {
            try
            {
                var comments = await _commentService.GetCommentsByPostIdAsync(postId);
                return Ok(comments);
            }
            catch (Exception)
            {
                return StatusCode(500, "获取评论列表时发生错误");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetComment(int id)
        {
            try
            {
                var comment = await _commentService.GetCommentByIdAsync(id);
                if (comment == null)
                    return NotFound("评论不存在");

                return Ok(comment);
            }
            catch (Exception)
            {
                return StatusCode(500, "获取评论详情时发生错误");
            }
        }

        [HttpGet("{id}/replies")]
        public async Task<IActionResult> GetReplies(int id)
        {
            try
            {
                var replies = await _commentService.GetRepliesByCommentIdAsync(id);
                return Ok(replies);
            }
            catch (Exception)
            {
                return StatusCode(500, "获取回复列表时发生错误");
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateComment([FromBody] CreateCommentRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst("nameid")?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized("用户未登录或身份无效");
                }
                var userId = int.Parse(userIdClaim);
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                if (!await _firewallService.CheckRateLimitAsync(ipAddress, "comment"))
                {
                    return StatusCode(429, "评论过于频繁，请稍后再试");
                }

                if (await _firewallService.ContainsSensitiveContentAsync(request.Content))
                {
                    await _firewallService.LogAccessAsync(ipAddress, "comment", false);
                    return BadRequest("评论内容包含不允许的内容");
                }

                if (await _firewallService.IsSpamContentAsync(request.Content))
                {
                    await _firewallService.LogAccessAsync(ipAddress, "comment", false);
                    return BadRequest("评论内容疑似垃圾信息");
                }

                var comment = new Comment
                {
                    Content = request.Content,
                    PostId = request.PostId,
                    ParentId = request.ParentCommentId,
                    AuthorId = userId,
                    IsAnonymous = request.IsAnonymous
                };

                var createdComment = await _commentService.CreateCommentAsync(comment);
                await _firewallService.LogAccessAsync(ipAddress, "comment", true);

                return CreatedAtAction(nameof(GetComment), new { id = createdComment.Id }, createdComment);
            }
            catch (Exception)
            {
                return StatusCode(500, "创建评论时发生错误");
            }
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateComment(int id, [FromBody] UpdateCommentRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst("nameid")?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized("用户未登录或身份无效");
                }
                var userId = int.Parse(userIdClaim);
                var comment = await _commentService.GetCommentByIdAsync(id);

                if (comment == null)
                    return NotFound("评论不存在");

                if (comment.AuthorId != userId && !User.IsInRole("Admin"))
                    return Forbid();

                if (await _firewallService.ContainsSensitiveContentAsync(request.Content))
                {
                    return BadRequest("评论内容包含不允许的内容");
                }

                comment.Content = request.Content;
                comment.IsAnonymous = request.IsAnonymous;

                await _commentService.UpdateCommentAsync(comment);
                return Ok(comment);
            }
            catch (Exception)
            {
                return StatusCode(500, "更新评论时发生错误");
            }
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComment(int id)
        {
            try
            {
                var userIdClaim = User.FindFirst("nameid")?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized("用户未登录或身份无效");
                }
                var userId = int.Parse(userIdClaim);
                var comment = await _commentService.GetCommentByIdAsync(id);

                if (comment == null)
                    return NotFound("评论不存在");

                if (comment.AuthorId != userId && !User.IsInRole("Admin"))
                    return Forbid();

                await _commentService.DeleteCommentAsync(id);
                return Ok(new { Message = "删除成功" });
            }
            catch (Exception)
            {
                return StatusCode(500, "删除评论时发生错误");
            }
        }

        [Authorize]
        [HttpPost("{id}/like")]
        public async Task<IActionResult> LikeComment(int id)
        {
            try
            {
                var userIdClaim = User.FindFirst("nameid")?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized("用户未登录或身份无效");
                }
                var userId = int.Parse(userIdClaim);
                var success = await _commentService.LikeCommentAsync(id, userId);

                if (!success)
                    return BadRequest("您已经点赞过这条评论了");

                return Ok(new { Message = "点赞成功" });
            }
            catch (Exception)
            {
                return StatusCode(500, "点赞时发生错误");
            }
        }

        [Authorize]
        [HttpDelete("{id}/like")]
        public async Task<IActionResult> UnlikeComment(int id)
        {
            try
            {
                var userIdClaim = User.FindFirst("nameid")?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized("用户未登录或身份无效");
                }
                var userId = int.Parse(userIdClaim);
                var success = await _commentService.UnlikeCommentAsync(id, userId);

                if (!success)
                    return BadRequest("您还没有点赞这条评论");

                return Ok(new { Message = "取消点赞成功" });
            }
            catch (Exception)
            {
                return StatusCode(500, "取消点赞时发生错误");
            }
        }

        [Authorize]
        [HttpPost("{id}/report")]
        public async Task<IActionResult> ReportComment(int id, [FromBody] ReportRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst("nameid")?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized("用户未登录或身份无效");
                }
                var userId = int.Parse(userIdClaim);
                var success = await _commentService.ReportCommentAsync(id, userId, request.Reason, request.Description);

                if (!success)
                    return BadRequest("举报失败");

                return Ok(new { Message = "举报成功" });
            }
            catch (Exception)
            {
                return StatusCode(500, "举报时发生错误");
            }
        }
    }

    public class CreateCommentRequest
    {
        [Required]
        public string Content { get; set; } = null!;
        public int PostId { get; set; }
        public int? ParentCommentId { get; set; }
        public bool IsAnonymous { get; set; }
    }

    public class UpdateCommentRequest
    {
        [Required]
        public string Content { get; set; } = null!;
        public bool IsAnonymous { get; set; }
    }
} 