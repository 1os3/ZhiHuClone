#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZhihuClone.Core.Models;
using ZhihuClone.Core.Interfaces;
using ZhihuClone.Infrastructure.Data;
using ZhihuClone.Core.Models.Content;
using Microsoft.Extensions.Logging;

namespace ZhihuClone.Infrastructure.Services
{
    public class PostService : IPostService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPostRepository _postRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<PostService> _logger;

        public PostService(
            ApplicationDbContext context, 
            IPostRepository postRepository, 
            IUnitOfWork unitOfWork,
            ILogger<PostService> logger)
        {
            _context = context;
            _postRepository = postRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Post?> GetByIdAsync(int id)
        {
            return await _postRepository.GetByIdAsync(id);
        }

        public async Task<List<Post>> GetAllAsync()
        {
            return await _postRepository.GetAllAsync();
        }

        public async Task<List<Post>> GetPagedAsync(int page = 1, int pageSize = 20)
        {
            return await _postRepository.GetPagedAsync(page, pageSize);
        }

        public async Task<List<Post>> GetByUserIdAsync(int userId)
        {
            return await _context.Posts
                .Where(p => p.AuthorId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Post>> GetByTopicIdAsync(int topicId)
        {
            return await _context.Posts
                .Where(p => p.Topics.Any(t => t.Id == topicId))
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Post>> SearchAsync(string searchTerm, int page = 1, int pageSize = 20)
        {
            var query = _context.Posts.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(p => 
                    p.Title.Contains(searchTerm) || 
                    p.Content.Contains(searchTerm));
            }

            return await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetTotalCountAsync(string? searchTerm = null)
        {
            var query = _context.Posts.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(p => 
                    p.Title.Contains(searchTerm) || 
                    p.Content.Contains(searchTerm));
            }

            return await query.CountAsync();
        }

        public async Task<Post> CreateAsync(Post post)
        {
            try
            {
                // 1. 验证基本数据
                if (post == null)
                {
                    _logger.LogError("尝试创建空的文章对象");
                    throw new ArgumentNullException(nameof(post));
                }

                if (string.IsNullOrWhiteSpace(post.Title))
                {
                    _logger.LogError("文章标题为空");
                    throw new ArgumentException("文章标题不能为空");
                }

                if (string.IsNullOrWhiteSpace(post.Content))
                {
                    _logger.LogError("文章内容为空");
                    throw new ArgumentException("文章内容不能为空");
                }

                if (post.Title.Length > 200)
                {
                    _logger.LogError($"文章标题超过长度限制: {post.Title.Length} > 200");
                    throw new ArgumentException("文章标题不能超过200个字符");
                }

                // 2. 验证作者
                var author = await _context.Users.FindAsync(post.AuthorId);
                if (author == null)
                {
                    _logger.LogError($"作者不存在: {post.AuthorId}");
                    throw new ArgumentException("作者不存在");
                }

                if (!author.IsActive)
                {
                    _logger.LogError($"作者账号已被禁用: {post.AuthorId}");
                    throw new ArgumentException("作者账号已被禁用");
                }

                // 3. 处理摘要
                if (string.IsNullOrEmpty(post.Summary))
                {
                    post.Summary = post.Content.Length > 500 
                        ? post.Content.Substring(0, 497) + "..."
                        : post.Content;
                }
                else if (post.Summary.Length > 500)
                {
                    post.Summary = post.Summary.Substring(0, 497) + "...";
                }

                // 4. 设置时间戳
                var now = DateTime.UtcNow;
                post.CreatedAt = now;
                post.UpdatedAt = now;

                // 5. 初始化计数器
                post.ViewCount = 0;
                post.LikeCount = 0;
                post.CommentCount = 0;
                post.CollectCount = 0;
                post.ShareCount = 0;

                // 6. 创建文章
                await _postRepository.AddAsync(post);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"成功创建文章: {post.Id}");
                return post;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建文章时发生错误");
                throw;
            }
        }

        public async Task<ServiceResult> UpdateAsync(Post post)
        {
            try
            {
                _context.Posts.Update(post);
                await _context.SaveChangesAsync();
                return new ServiceResult { Succeeded = true };
            }
            catch (Exception ex)
            {
                return new ServiceResult 
                { 
                    Succeeded = false,
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task DeleteAsync(int id)
        {
            await _postRepository.DeleteAsync(id);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<bool> LikeAsync(int postId, int userId)
        {
            var post = await GetByIdAsync(postId);
            if (post == null) return false;

            var like = await _context.PostLikes
                .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);

            if (like != null) return false;

            await _context.PostLikes.AddAsync(new PostLike
            {
                PostId = postId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            });

            post.LikeCount++;
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UnlikeAsync(int postId, int userId)
        {
            var post = await GetByIdAsync(postId);
            if (post == null) return false;

            var like = await _context.PostLikes
                .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);

            if (like == null) return false;

            _context.PostLikes.Remove(like);
            post.LikeCount = Math.Max(0, post.LikeCount - 1);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsLikedByUserAsync(int postId, int userId)
        {
            return await _context.PostLikes
                .AnyAsync(l => l.PostId == postId && l.UserId == userId);
        }

        public async Task<List<Post>> GetLatestAsync(int count = 10)
        {
            return await _context.Posts
                .OrderByDescending(p => p.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<Post>> GetTrendingAsync(int count = 10)
        {
            return await _context.Posts
                .OrderByDescending(p => p.ViewCount)
                .ThenByDescending(p => p.LikeCount)
                .ThenByDescending(p => p.CommentCount)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<Post>> GetUserCollectionsAsync(int userId, int page = 1, int pageSize = 10)
        {
            return await _context.Posts
                .Where(p => p.CollectedUsers.Any(u => u.Id == userId))
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<Post>> GetPostsByTopicAsync(int topicId, int page = 1, int pageSize = 10)
        {
            return await _context.Posts
                .Where(p => p.Topics.Any(t => t.Id == topicId))
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<Post>> GetAllPostsAsync(int page = 1, int pageSize = 10)
        {
            return await _context.Posts
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<Post>> SearchPostsAsync(string keyword, int page = 1, int pageSize = 20)
        {
            return await _context.Posts
                .Where(p => p.Title.Contains(keyword) || p.Content.Contains(keyword))
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<PostReport> ReportPostAsync(int postId, int reporterId, string reason, string description)
        {
            var report = new PostReport
            {
                PostId = postId,
                ReporterId = reporterId,
                Reason = reason,
                Description = description,
                IsProcessed = false,
                CreatedAt = DateTime.UtcNow
            };

            await _context.PostReports.AddAsync(report);
            await _unitOfWork.SaveChangesAsync();
            return report;
        }

        public async Task<List<PostReport>> GetPostReportsAsync(int page = 1, int pageSize = 10)
        {
            return await _context.PostReports
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<PostReport>> GetUnprocessedReportsAsync(int page = 1, int pageSize = 10)
        {
            return await _context.PostReports
                .Where(r => !r.IsProcessed)
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task ProcessReportAsync(int reportId, int processedByUserId, string processResult)
        {
            var report = await _context.PostReports.FindAsync(reportId);
            if (report != null)
            {
                report.IsProcessed = true;
                report.ProcessedByUserId = processedByUserId;
                report.ProcessResult = processResult;
                report.ProcessedAt = DateTime.UtcNow;
                await _unitOfWork.SaveChangesAsync();
            }
        }

        public async Task<int> GetPostCountAsync()
        {
            return await _context.Posts.CountAsync();
        }

        public async Task<bool> IsPostLikedByUserAsync(int postId, string? username)
        {
            if (string.IsNullOrEmpty(username)) return false;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == username);
            if (user == null) return false;

            return await _context.PostLikes
                .AnyAsync(l => l.PostId == postId && l.UserId == user.Id);
        }

        public async Task IncrementViewCountAsync(int postId)
        {
            var post = await _context.Posts.FindAsync(postId);
            if (post != null)
            {
                post.ViewCount++;
                await _unitOfWork.SaveChangesAsync();
            }
        }

        public async Task<List<Post>> GetUserPostsAsync(int userId, int page = 1, int pageSize = 10)
        {
            return await _postRepository.GetUserPostsAsync(userId, page, pageSize);
        }

        public async Task<List<Post>> GetTrendingPostsAsync(int count = 10)
        {
            return await _postRepository.GetTrendingPostsAsync(count);
        }

        public async Task<bool> LikePostAsync(int postId, int userId)
        {
            return await LikeAsync(postId, userId);
        }

        public async Task<bool> UnlikePostAsync(int postId, int userId)
        {
            return await UnlikeAsync(postId, userId);
        }

        public async Task<int> GetUserPostCountAsync(int userId)
        {
            return await _context.Posts
                .Where(p => p.AuthorId == userId && p.Status != PostStatus.Draft)
                .CountAsync();
        }

        public async Task<List<Post>> GetUserRecentPostsAsync(int userId, int count)
        {
            return await _context.Posts
                .Where(p => p.AuthorId == userId && p.Status != PostStatus.Draft)
                .OrderByDescending(p => p.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<Post>> GetUserDraftsAsync(int userId)
        {
            return await _context.Posts
                .Where(p => p.AuthorId == userId && p.Status == PostStatus.Draft)
                .OrderByDescending(p => p.UpdatedAt)
                .ToListAsync();
        }

        public async Task<ServiceResult> AddTopicAsync(int postId, int topicId)
        {
            try
            {
                var post = await _context.Posts.FindAsync(postId);
                var topic = await _context.Topics.FindAsync(topicId);

                if (post == null || topic == null)
                {
                    return new ServiceResult 
                    { 
                        Succeeded = false,
                        Errors = new List<string> { "文章或话题不存在" }
                    };
                }

                if (!post.Topics.Any(t => t.Id == topicId))
                {
                    post.Topics.Add(topic);
                    await _context.SaveChangesAsync();
                }

                return new ServiceResult { Succeeded = true };
            }
            catch (Exception ex)
            {
                return new ServiceResult 
                { 
                    Succeeded = false,
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ServiceResult> RemoveTopicAsync(int postId, int topicId)
        {
            try
            {
                var post = await _context.Posts
                    .Include(p => p.Topics)
                    .FirstOrDefaultAsync(p => p.Id == postId);

                if (post == null)
                {
                    return new ServiceResult 
                    { 
                        Succeeded = false,
                        Errors = new List<string> { "文章不存在" }
                    };
                }

                var topic = post.Topics.FirstOrDefault(t => t.Id == topicId);
                if (topic != null)
                {
                    post.Topics.Remove(topic);
                    await _context.SaveChangesAsync();
                }

                return new ServiceResult { Succeeded = true };
            }
            catch (Exception ex)
            {
                return new ServiceResult 
                { 
                    Succeeded = false,
                    Errors = new List<string> { ex.Message }
                };
            }
        }
    }
} 