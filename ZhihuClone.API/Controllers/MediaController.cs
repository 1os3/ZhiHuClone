using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using ZhihuClone.Core.Models;
using ZhihuClone.Core.Services.Interfaces;
using System.Security.Claims;
using ZhihuClone.Core.Interfaces;
using ZhihuClone.Core.Interfaces.Security;

namespace ZhihuClone.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MediaController : ControllerBase
    {
        private readonly IMediaService _mediaService;
        private readonly IFirewallService _firewallService;
        private const long MaxFileSize = 2L * 1024 * 1024 * 1024; // 2GB
        private const int MaxImageResolution = 3840; // 4K
        private const int MaxVideoResolution = 3840; // 4K

        public MediaController(
            IMediaService mediaService,
            IFirewallService firewallService)
        {
            _mediaService = mediaService;
            _firewallService = firewallService;
        }

        [Authorize]
        [HttpPost("upload")]
        public async Task<IActionResult> UploadMedia(IFormFile file)
        {
            try
            {
                var userIdClaim = User.FindFirst("nameid")?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized("用户未登录或身份无效");
                }
                var userId = int.Parse(userIdClaim);

                if (file == null || file.Length == 0)
                    return BadRequest("未选择文件");

                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                if (!await _firewallService.CheckRateLimitAsync(ipAddress, "upload"))
                {
                    return StatusCode(429, "上传过于频繁，请稍后再试");
                }

                if (!await _firewallService.ValidateFileContentType(file, file.ContentType))
                {
                    await _firewallService.LogAccessAsync(ipAddress, "upload", false);
                    return BadRequest("不支持的文件类型");
                }

                var media = await _mediaService.UploadMediaAsync(file, userId);
                await _firewallService.LogAccessAsync(ipAddress, "upload", true);

                return Ok(new { FilePath = media.Url });
            }
            catch (Exception)
            {
                return StatusCode(500, "上传文件时发生错误");
            }
        }

        [Authorize]
        [HttpPost("upload-multiple")]
        public async Task<IActionResult> UploadMultipleMedia([FromForm] List<IFormFile> files, [FromForm] int postId)
        {
            try
            {
                var userIdClaim = User.FindFirst("nameid")?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized("用户未登录或身份无效");
                }
                var userId = int.Parse(userIdClaim);

                if (files == null || !files.Any())
                    return BadRequest("未选择文件");

                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                if (!await _firewallService.CheckRateLimitAsync(ipAddress, "upload"))
                {
                    return StatusCode(429, "上传过于频繁，请稍后再试");
                }

                foreach (var file in files)
                {
                    if (!await _firewallService.ValidateFileContentType(file, file.ContentType))
                    {
                        await _firewallService.LogAccessAsync(ipAddress, "upload", false);
                        return BadRequest($"不支持的文件类型: {file.FileName}");
                    }
                }

                var mediaList = await _mediaService.UploadMultipleMediaAsync(files, userId, postId);
                await _firewallService.LogAccessAsync(ipAddress, "upload", true);

                return Ok(mediaList.Select(m => new { FilePath = m.Url }));
            }
            catch (Exception)
            {
                return StatusCode(500, "上传文件时发生错误");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetMedia(int id)
        {
            try
            {
                var media = await _mediaService.GetMediaByIdAsync(id);
                if (media == null)
                    return NotFound("文件不存在");

                return Ok(media);
            }
            catch (Exception)
            {
                return StatusCode(500, "获取文件信息时发生错误");
            }
        }

        [HttpGet("post/{postId}")]
        public async Task<IActionResult> GetMediaByPost(int postId)
        {
            try
            {
                var mediaList = await _mediaService.GetMediaByPostIdAsync(postId);
                return Ok(mediaList);
            }
            catch (Exception)
            {
                return StatusCode(500, "获取文件列表时发生错误");
            }
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMedia(int id)
        {
            try
            {
                var userIdClaim = User.FindFirst("nameid")?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized("用户未登录或身份无效");
                }
                var userId = int.Parse(userIdClaim);

                var media = await _mediaService.GetMediaByIdAsync(id);
                if (media == null)
                    return NotFound("文件不存在");

                if (media.CreatedByUserId != userId && !User.IsInRole("Admin"))
                    return Forbid();

                var success = await _mediaService.DeleteMediaAsync(id);
                if (!success)
                    return BadRequest("删除文件失败");

                return Ok(new { Message = "文件已删除" });
            }
            catch (Exception)
            {
                return StatusCode(500, "删除文件时发生错误");
            }
        }

        private bool IsImageFile(string extension)
        {
            return extension switch
            {
                ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" => true,
                _ => false
            };
        }

        private bool IsVideoFile(string extension)
        {
            return extension switch
            {
                ".mp4" or ".webm" or ".mov" or ".avi" => true,
                _ => false
            };
        }
    }
} 