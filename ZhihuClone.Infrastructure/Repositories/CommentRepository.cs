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
    public class CommentRepository : BaseRepository<Comment>, ICommentRepository
    {
        private new readonly ApplicationDbContext _context;

        public CommentRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public override async Task<Comment?> GetByIdAsync(int id)
        {
            return await _context.Comments
                .Include(c => c.Author)
                .Include(c => c.Parent)
                .Include(c => c.Replies)
                .Include(c => c.LikedUsers)
                .Include(c => c.Media)
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
        }

        public override async Task<List<Comment>> GetAllAsync()
        {
            return await _context.Comments
                .Include(c => c.Author)
                .Include(c => c.Parent)
                .Include(c => c.Replies)
                .Include(c => c.LikedUsers)
                .Include(c => c.Media)
                .Where(c => !c.IsDeleted)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Comment>> GetByPostIdAsync(int postId)
        {
            return await _context.Comments
                .Include(c => c.Author)
                .Include(c => c.Parent)
                .Include(c => c.Replies)
                .Include(c => c.LikedUsers)
                .Include(c => c.Media)
                .Where(c => c.PostId == postId && !c.IsDeleted)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Comment>> GetByUserIdAsync(int userId)
        {
            return await _context.Comments
                .Include(c => c.Author)
                .Include(c => c.Parent)
                .Include(c => c.Replies)
                .Include(c => c.LikedUsers)
                .Include(c => c.Media)
                .Where(c => c.AuthorId == userId && !c.IsDeleted)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Comment>> GetByUserIdAsync(int userId, int page, int pageSize)
        {
            return await _context.Comments
                .Where(c => c.AuthorId == userId && !c.IsDeleted)
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<Comment>> GetRepliesAsync(int commentId)
        {
            return await _context.Comments
                .Include(c => c.Author)
                .Include(c => c.Parent)
                .Include(c => c.LikedUsers)
                .Include(c => c.Media)
                .Where(c => c.ParentId == commentId && !c.IsDeleted)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Comment>> GetPagedAsync(int page = 1, int pageSize = 10)
        {
            return await _context.Comments
                .Include(c => c.Author)
                .Include(c => c.Parent)
                .Include(c => c.Replies)
                .Include(c => c.LikedUsers)
                .Include(c => c.Media)
                .Where(c => !c.IsDeleted)
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<Comment>> SearchAsync(string searchTerm, int page = 1, int pageSize = 10)
        {
            return await _context.Comments
                .Include(c => c.Author)
                .Include(c => c.Parent)
                .Include(c => c.Replies)
                .Include(c => c.LikedUsers)
                .Include(c => c.Media)
                .Where(c => !c.IsDeleted && c.Content.Contains(searchTerm))
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetTotalCountAsync(string? searchTerm = null)
        {
            var query = _context.Comments.Where(c => !c.IsDeleted);
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(c => c.Content.Contains(searchTerm));
            }
            return await query.CountAsync();
        }

        public override async Task<Comment> AddAsync(Comment comment)
        {
            comment.CreatedAt = DateTime.UtcNow;
            comment.UpdatedAt = DateTime.UtcNow;
            await _context.Comments.AddAsync(comment);
            await _context.SaveChangesAsync();
            return comment;
        }

        public override async Task<Comment> UpdateAsync(Comment comment)
        {
            comment.UpdatedAt = DateTime.UtcNow;
            _context.Comments.Update(comment);
            await _context.SaveChangesAsync();
            return comment;
        }

        public async Task DeleteAsync(int id)
        {
            var comment = await GetByIdAsync(id);
            if (comment != null)
            {
                comment.IsDeleted = true;
                comment.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> IsLikedAsync(int userId, int commentId)
        {
            return await _context.Comments
                .Include(c => c.LikedUsers)
                .AnyAsync(c => c.Id == commentId && c.LikedUsers.Any(u => u.Id == userId));
        }

        public async Task<int> GetLikeCountAsync(int commentId)
        {
            return await _context.Comments
                .Include(c => c.LikedUsers)
                .Where(c => c.Id == commentId)
                .Select(c => c.LikedUsers.Count)
                .FirstOrDefaultAsync();
        }

        public async Task<int> GetReplyCountAsync(int commentId)
        {
            return await _context.Comments.CountAsync(c => c.ParentId == commentId && !c.IsDeleted);
        }

        public async Task<bool> IsLikedByUserAsync(int commentId, int userId)
        {
            return await _context.Comments
                .Include(c => c.LikedUsers)
                .AnyAsync(c => c.Id == commentId && c.LikedUsers.Any(u => u.Id == userId));
        }

        public async Task<List<Comment>> GetPagedByPostAsync(int postId, int page, int pageSize)
        {
            return await _context.Comments
                .Include(c => c.Author)
                .Include(c => c.Parent)
                .Include(c => c.Replies)
                .Include(c => c.LikedUsers)
                .Include(c => c.Media)
                .Where(c => c.PostId == postId && !c.IsDeleted)
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<Comment>> GetTopCommentsByPostAsync(int postId, int count)
        {
            return await _context.Comments
                .Include(c => c.Author)
                .Include(c => c.Parent)
                .Include(c => c.Replies)
                .Include(c => c.LikedUsers)
                .Include(c => c.Media)
                .Where(c => c.PostId == postId && !c.IsDeleted)
                .OrderByDescending(c => c.LikedUsers.Count)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<Comment>> GetRecentCommentsByPostAsync(int postId, int count)
        {
            return await _context.Comments
                .Include(c => c.Author)
                .Include(c => c.Parent)
                .Include(c => c.Replies)
                .Include(c => c.LikedUsers)
                .Include(c => c.Media)
                .Where(c => c.PostId == postId && !c.IsDeleted)
                .OrderByDescending(c => c.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public override IQueryable<Comment> Query()
        {
            return _context.Comments
                .Include(c => c.Author)
                .Include(c => c.Parent)
                .Include(c => c.Replies)
                .Include(c => c.LikedUsers)
                .Include(c => c.Media)
                .Where(c => !c.IsDeleted);
        }

        public async Task<int> CountByPostAsync(int postId)
        {
            return await _context.Comments
                .CountAsync(c => c.PostId == postId && !c.IsDeleted);
        }

        public async Task<int> CountByUserAsync(int userId)
        {
            return await _context.Comments
                .CountAsync(c => c.AuthorId == userId && !c.IsDeleted);
        }
    }
} 