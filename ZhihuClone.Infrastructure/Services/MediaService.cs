using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using FFMpegCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using ZhihuClone.Core.Models.Content;
using ZhihuClone.Core.Interfaces;
using System.Linq;
using System.Data;
using Dapper;
using ZhihuClone.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ZhihuClone.Infrastructure.Services
{
    public class MediaService : IMediaService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMediaRepository _mediaRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;
        private readonly string _uploadPath;
        private readonly string? _baseUrl;
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public MediaService(
            ApplicationDbContext context,
            IMediaRepository mediaRepository,
            IUnitOfWork unitOfWork,
            IConfiguration configuration,
            IDbConnectionFactory dbConnectionFactory)
        {
            _context = context;
            _mediaRepository = mediaRepository;
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            _baseUrl = _configuration["BaseUrl"];
            _dbConnectionFactory = dbConnectionFactory;

            // 确保上传目录存在
            if (!Directory.Exists(_uploadPath))
            {
                Directory.CreateDirectory(_uploadPath);
            }

            // 确保头像目录存在
            var avatarPath = Path.Combine(_uploadPath, "avatars");
            if (!Directory.Exists(avatarPath))
            {
                Directory.CreateDirectory(avatarPath);
            }
        }

        public async Task<Media?> GetByIdAsync(int id)
        {
            return await _context.Set<Media>().FindAsync(id);
        }

        public async Task<List<Media>> GetAllAsync()
        {
            return await _context.Set<Media>()
                .Where(m => !m.IsDeleted)
                .ToListAsync();
        }

        public async Task<List<Media>> GetByPostIdAsync(int postId)
        {
            return await _context.Set<Media>()
                .Where(m => m.Posts.Any(p => p.Id == postId) && !m.IsDeleted)
                .ToListAsync();
        }

        public async Task<List<Media>> GetByUserIdAsync(int userId)
        {
            return await _context.Set<Media>()
                .Where(m => m.CreatedByUserId == userId && !m.IsDeleted)
                .ToListAsync();
        }

        public async Task<List<Media>> GetPagedAsync(int page = 1, int pageSize = 20)
        {
            return await _context.Set<Media>()
                .Where(m => !m.IsDeleted)
                .OrderByDescending(m => m.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<Media>> SearchAsync(string searchTerm, int page = 1, int pageSize = 10)
        {
            return await _context.Set<Media>()
                .Where(m => !m.IsDeleted && 
                    (m.FileName.Contains(searchTerm) || 
                     m.Description != null && m.Description.Contains(searchTerm)))
                .OrderByDescending(m => m.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetTotalCountAsync(string? searchTerm = null)
        {
            var query = _context.Set<Media>().Where(m => !m.IsDeleted);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(m => 
                    m.FileName.Contains(searchTerm) || 
                    m.Description != null && m.Description.Contains(searchTerm));
            }

            return await query.CountAsync();
        }

        public async Task<Media> CreateAsync(Media media)
        {
            media.CreatedAt = DateTime.UtcNow;
            await _context.Set<Media>().AddAsync(media);
            await _unitOfWork.SaveChangesAsync();
            return media;
        }

        public async Task UpdateAsync(Media media)
        {
            media.UpdatedAt = DateTime.UtcNow;
            _context.Entry(media).State = EntityState.Modified;
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var media = await GetByIdAsync(id);
            if (media != null)
            {
                media.IsDeleted = true;
                media.UpdatedAt = DateTime.UtcNow;
                _context.Entry(media).State = EntityState.Modified;
                await _unitOfWork.SaveChangesAsync();
            }
        }

        public async Task<bool> IsValidTypeAsync(string contentType)
        {
            var validTypes = new[] { "image/jpeg", "image/png", "image/gif", "video/mp4", "video/webm" };
            return await Task.FromResult(Array.Exists(validTypes, type => type.Equals(contentType, StringComparison.OrdinalIgnoreCase)));
        }

        public async Task<string?> GetUrlAsync(int mediaId)
        {
            var media = await GetByIdAsync(mediaId);
            return media?.Url;
        }

        public async Task<Media> UploadMediaAsync(IFormFile file, int userId)
        {
            var media = new Media
            {
                FileName = file.FileName,
                Size = file.Length,
                MimeType = file.ContentType,
                Extension = Path.GetExtension(file.FileName),
                Type = GetMediaType(file.ContentType),
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId
            };

            await _context.Set<Media>().AddAsync(media);
            await _unitOfWork.SaveChangesAsync();
            return media;
        }

        public async Task<List<Media>> UploadMultipleMediaAsync(IList<IFormFile> files, int userId, int? postId = null)
        {
            var mediaList = new List<Media>();
            foreach (var file in files)
            {
                var media = await UploadMediaAsync(file, userId);
                if (postId.HasValue)
                {
                    await AssociateMediaWithPostAsync(media.Id, postId.Value);
                }
                mediaList.Add(media);
            }
            return mediaList;
        }

        public async Task<Media?> GetMediaByIdAsync(int id)
        {
            return await GetByIdAsync(id);
        }

        public async Task<List<Media>> GetMediaByPostIdAsync(int postId)
        {
            return await GetByPostIdAsync(postId);
        }

        public async Task<bool> DeleteMediaAsync(int id)
        {
            var media = await GetByIdAsync(id);
            if (media == null)
                return false;

            media.IsDeleted = true;
            media.UpdatedAt = DateTime.UtcNow;
            _context.Entry(media).State = EntityState.Modified;
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AssociateMediaWithPostAsync(int mediaId, int postId)
        {
            var media = await GetByIdAsync(mediaId);
            var post = await _context.Set<Post>().FindAsync(postId);

            if (media == null || post == null)
                return false;

            media.Posts.Add(post);
            _context.Entry(media).State = EntityState.Modified;
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        private async Task GenerateThumbnailAsync(string sourcePath, string targetPath)
        {
            using var image = await Image.LoadAsync(sourcePath);
            var ratio = Math.Min(1920.0 / image.Width, 1080.0 / image.Height);
            var newWidth = (int)(image.Width * ratio);
            var newHeight = (int)(image.Height * ratio);

            image.Mutate(x => x.Resize(newWidth, newHeight));
            await image.SaveAsync(targetPath);
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

        private MediaType GetMediaType(string contentType)
        {
            return contentType.ToLower() switch
            {
                var t when t.StartsWith("image/") => MediaType.Image,
                var t when t.StartsWith("video/") => MediaType.Video,
                var t when t.StartsWith("audio/") => MediaType.Audio,
                _ => MediaType.Other
            };
        }

        public async Task<string> SaveFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("文件无效");
            }

            // 生成唯一的文件名
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(_uploadPath, fileName);

            // 保存文件
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // 如果是图片，生成缩略图
            if (IsImageFile(Path.GetExtension(fileName)))
            {
                var thumbnailPath = Path.Combine(_uploadPath, "thumbnails", fileName);
                await GenerateThumbnailAsync(filePath, thumbnailPath);
            }

            // 返回文件的相对路径
            return $"/uploads/{fileName}";
        }

        public async Task<bool> DeleteFileAsync(string filePath)
        {
            try
            {
                // 获取完整的文件路径
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", filePath.TrimStart('/'));
                
                // 检查文件是否存在
                if (!File.Exists(fullPath))
                {
                    return false;
                }

                // 删除文件
                File.Delete(fullPath);

                // 如果是图片，删除对应的缩略图
                if (IsImageFile(Path.GetExtension(filePath)))
                {
                    var thumbnailPath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        "uploads",
                        "thumbnails",
                        Path.GetFileName(filePath)
                    );
                    if (File.Exists(thumbnailPath))
                    {
                        File.Delete(thumbnailPath);
                    }
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<string> UploadAvatarAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("文件无效");
            }

            // 验证文件类型
            var contentType = file.ContentType.ToLower();
            if (!contentType.StartsWith("image/"))
            {
                throw new ArgumentException("只能上传图片文件");
            }

            // 生成唯一的文件名
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(_uploadPath, "avatars", fileName);

            // 保存文件
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // 生成缩略图
            using (var image = await Image.LoadAsync(filePath))
            {
                // 调整图片大小为 200x200
                image.Mutate(x => x
                    .Resize(new ResizeOptions
                    {
                        Size = new Size(200, 200),
                        Mode = ResizeMode.Crop
                    }));

                // 保存调整后的图片
                await image.SaveAsync(filePath);
            }

            // 返回文件的相对路径
            return $"/uploads/avatars/{fileName}";
        }
    }
} 