using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZhihuClone.Core.Interfaces;
using ZhihuClone.Core.Models.Content;
using ZhihuClone.Infrastructure.Data;

namespace ZhihuClone.Infrastructure.Repositories
{
    public class PostRepository : IPostRepository
    {
        private readonly ApplicationDbContext _context;

        public PostRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Post?> GetByIdAsync(int id)
        {
            return await _context.Posts
                .Include(p => p.Author)
                .Include(p => p.Topics)
                .Include(p => p.Media)
                .FirstOrDefaultAsync(p => p.Id == id && p.Status != PostStatus.Deleted);
        }

        public async Task<List<Post>> GetAllAsync()
        {
            return await _context.Posts
                .Include(p => p.Author)
                .Include(p => p.Topics)
                .Where(p => p.Status != PostStatus.Deleted)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Post>> GetByUserIdAsync(int userId)
        {
            return await _context.Posts
                .Include(p => p.Topics)
                .Where(p => p.AuthorId == userId && p.Status != PostStatus.Deleted)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Post>> GetByTopicIdAsync(int topicId)
        {
            return await _context.Posts
                .Include(p => p.Author)
                .Where(p => p.Topics.Any(t => t.Id == topicId) && p.Status != PostStatus.Deleted)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Post>> GetRecommendedAsync(int page = 1, int pageSize = 10)
        {
            return await _context.Posts
                .Include(p => p.Author)
                .Include(p => p.Topics)
                .Where(p => p.Status == PostStatus.Published)
                .OrderByDescending(p => p.ViewCount)
                .ThenByDescending(p => p.LikeCount)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<Post>> GetTrendingAsync(int page = 1, int pageSize = 10)
        {
            return await _context.Posts
                .Include(p => p.Author)
                .Include(p => p.Topics)
                .Where(p => p.Status == PostStatus.Published)
                .OrderByDescending(p => p.LikeCount)
                .ThenByDescending(p => p.CommentCount)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<Post>> GetLatestAsync(int page = 1, int pageSize = 10)
        {
            return await _context.Posts
                .Include(p => p.Author)
                .Include(p => p.Topics)
                .Where(p => p.Status == PostStatus.Published)
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<Post> AddAsync(Post post)
        {
            await _context.Posts.AddAsync(post);
            await _context.SaveChangesAsync();
            return post;
        }

        public async Task<Post> UpdateAsync(Post post)
        {
            _context.Posts.Update(post);
            await _context.SaveChangesAsync();
            return post;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null) return false;

            post.Status = PostStatus.Deleted;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> CountByUserAsync(int userId)
        {
            return await _context.Posts
                .CountAsync(p => p.AuthorId == userId && p.Status != PostStatus.Deleted);
        }

        public async Task<int> CountByTopicAsync(int topicId)
        {
            return await _context.Posts
                .CountAsync(p => p.Topics.Any(t => t.Id == topicId) && p.Status != PostStatus.Deleted);
        }

        public async Task<List<Post>> SearchAsync(string searchTerm, int page = 1, int pageSize = 10)
        {
            return await _context.Posts
                .Include(p => p.Author)
                .Include(p => p.Topics)
                .Where(p => p.Status == PostStatus.Published &&
                    (p.Title.Contains(searchTerm) ||
                     p.Content.Contains(searchTerm) ||
                     p.Summary.Contains(searchTerm)))
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<Post>> GetPagedAsync(int page = 1, int pageSize = 10)
        {
            return await _context.Posts
                .Include(p => p.Author)
                .Include(p => p.Topics)
                .Where(p => p.Status == PostStatus.Published)
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<bool> IsLikedAsync(int userId, int postId)
        {
            return await _context.Users
                .Where(u => u.Id == userId)
                .SelectMany(u => u.LikedPosts)
                .AnyAsync(p => p.Id == postId);
        }

        public async Task<bool> IsCollectedAsync(int userId, int postId)
        {
            return await _context.Posts
                .Where(p => p.Id == postId)
                .SelectMany(p => p.CollectedUsers)
                .AnyAsync(u => u.Id == userId);
        }

        public async Task<int> GetLikeCountAsync(int postId)
        {
            var post = await _context.Posts.FindAsync(postId);
            return post?.LikeCount ?? 0;
        }

        public async Task<int> GetCollectCountAsync(int postId)
        {
            var post = await _context.Posts.FindAsync(postId);
            return post?.CollectCount ?? 0;
        }

        public async Task<int> GetCommentCountAsync(int postId)
        {
            var post = await _context.Posts.FindAsync(postId);
            return post?.CommentCount ?? 0;
        }

        public async Task<int> GetViewCountAsync(int postId)
        {
            var post = await _context.Posts.FindAsync(postId);
            return post?.ViewCount ?? 0;
        }

        public async Task IncrementViewCountAsync(int postId)
        {
            var post = await _context.Posts.FindAsync(postId);
            if (post != null)
            {
                post.ViewCount++;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<Post?> SingleOrDefaultAsync(Expression<Func<Post, bool>> predicate)
        {
            return await _context.Posts.SingleOrDefaultAsync(predicate);
        }

        public async Task<Post?> FirstOrDefaultAsync(Expression<Func<Post, bool>> predicate)
        {
            return await _context.Posts.FirstOrDefaultAsync(predicate);
        }

        public async Task<bool> AnyAsync(Expression<Func<Post, bool>> predicate)
        {
            return await _context.Posts.AnyAsync(predicate);
        }

        public async Task<int> CountAsync(Expression<Func<Post, bool>> predicate)
        {
            return await _context.Posts.CountAsync(predicate);
        }

        public async Task<List<Post>> FindAsync(Expression<Func<Post, bool>> predicate)
        {
            return await _context.Posts.Where(predicate).ToListAsync();
        }

        public async Task<IEnumerable<Post>> AddRangeAsync(IEnumerable<Post> entities)
        {
            await _context.Posts.AddRangeAsync(entities);
            await _context.SaveChangesAsync();
            return entities;
        }

        public Post Update(Post post)
        {
            _context.Posts.Update(post);
            _context.SaveChanges();
            return post;
        }

        public void Remove(Post post)
        {
            _context.Posts.Remove(post);
            _context.SaveChanges();
        }

        public void RemoveRange(IEnumerable<Post> entities)
        {
            _context.Posts.RemoveRange(entities);
            _context.SaveChanges();
        }

        public IQueryable<Post> Query()
        {
            return _context.Posts;
        }

        public async Task RemoveAsync(Post post)
        {
            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveAsync(int id)
        {
            var post = await GetByIdAsync(id);
            if (post != null)
            {
                _context.Posts.Remove(post);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> GetTotalCountAsync(string? searchTerm = null)
        {
            var query = _context.Posts.Where(p => p.Status == PostStatus.Published);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(p =>
                    p.Title.Contains(searchTerm) ||
                    p.Content.Contains(searchTerm) ||
                    p.Summary.Contains(searchTerm));
            }

            return await query.CountAsync();
        }

        public async Task<List<Post>> GetUserPostsAsync(int userId, int page = 1, int pageSize = 10)
        {
            return await _context.Posts
                .Include(p => p.Author)
                .Include(p => p.Topics)
                .Where(p => p.AuthorId == userId && p.Status == PostStatus.Published)
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<Post>> GetTrendingPostsAsync(int count = 10)
        {
            return await _context.Posts
                .Include(p => p.Author)
                .Include(p => p.Topics)
                .Where(p => p.Status == PostStatus.Published)
                .OrderByDescending(p => p.ViewCount)
                .ThenByDescending(p => p.LikeCount)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<Post>> GetLatestPostsAsync(int count = 10)
        {
            return await _context.Posts
                .Include(p => p.Author)
                .Include(p => p.Topics)
                .Where(p => p.Status == PostStatus.Published)
                .OrderByDescending(p => p.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<Post>> GetUserCollectionsAsync(int userId, int page = 1, int pageSize = 10)
        {
            return await _context.Posts
                .Include(p => p.Author)
                .Include(p => p.Topics)
                .Where(p => p.CollectedUsers.Any(u => u.Id == userId) && p.Status == PostStatus.Published)
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<Post>> GetPostsByTopicAsync(int topicId, int page = 1, int pageSize = 10)
        {
            return await _context.Posts
                .Include(p => p.Author)
                .Include(p => p.Topics)
                .Where(p => p.Status == PostStatus.Published && p.Topics.Any(t => t.Id == topicId))
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<Post>> GetAllPostsAsync(int page = 1, int pageSize = 10)
        {
            return await _context.Posts
                .Include(p => p.Author)
                .Include(p => p.Topics)
                .Where(p => p.Status != PostStatus.Deleted)
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
    }
} 