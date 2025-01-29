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
    public class TopicRepository : BaseRepository<Topic>, ITopicRepository
    {
        private new readonly ApplicationDbContext _context;

        public TopicRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public override async Task<Topic?> GetByIdAsync(int id)
        {
            return await _context.Topics
                .Include(t => t.Parent)
                .Include(t => t.Children)
                .Include(t => t.Posts)
                .Include(t => t.Followers)
                .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);
        }

        public override async Task<List<Topic>> GetAllAsync()
        {
            return await _context.Topics
                .Include(t => t.Parent)
                .Include(t => t.Children)
                .Include(t => t.Posts)
                .Include(t => t.Followers)
                .Where(t => !t.IsDeleted)
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        public async Task<List<Topic>> GetByUserIdAsync(int userId)
        {
            return await _context.Topics
                .Include(t => t.Parent)
                .Include(t => t.Children)
                .Include(t => t.Posts)
                .Include(t => t.Followers)
                .Where(t => t.Followers.Any(f => f.Id == userId) && !t.IsDeleted)
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        public async Task<List<Topic>> GetByPostIdAsync(int postId)
        {
            return await _context.Topics
                .Include(t => t.Parent)
                .Include(t => t.Children)
                .Include(t => t.Posts)
                .Include(t => t.Followers)
                .Where(t => t.Posts.Any(p => p.Id == postId) && !t.IsDeleted)
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        public async Task<List<Topic>> GetHotTopicsAsync(int count)
        {
            return await _context.Topics
                .Include(t => t.Parent)
                .Include(t => t.Children)
                .Include(t => t.Posts)
                .Include(t => t.Followers)
                .Where(t => !t.IsDeleted)
                .OrderByDescending(t => t.Followers.Count)
                .ThenByDescending(t => t.Posts.Count)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<Topic>> GetChildrenAsync(int parentId)
        {
            return await _context.Topics
                .Include(t => t.Parent)
                .Include(t => t.Children)
                .Include(t => t.Posts)
                .Include(t => t.Followers)
                .Where(t => t.ParentId == parentId && !t.IsDeleted)
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        public async Task<List<Topic>> GetPagedAsync(int page = 1, int pageSize = 10)
        {
            return await _context.Topics
                .Include(t => t.Parent)
                .Include(t => t.Children)
                .Include(t => t.Posts)
                .Include(t => t.Followers)
                .Where(t => !t.IsDeleted)
                .OrderBy(t => t.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<Topic>> SearchAsync(string searchTerm, int page = 1, int pageSize = 10)
        {
            return await _context.Topics
                .Include(t => t.Parent)
                .Include(t => t.Children)
                .Include(t => t.Posts)
                .Include(t => t.Followers)
                .Where(t => !t.IsDeleted && (t.Name.Contains(searchTerm) || t.Description.Contains(searchTerm)))
                .OrderBy(t => t.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetTotalCountAsync(string? searchTerm = null)
        {
            var query = _context.Topics.Where(t => !t.IsDeleted);
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(t => t.Name.Contains(searchTerm) || t.Description.Contains(searchTerm));
            }
            return await query.CountAsync();
        }

        public override async Task<Topic> AddAsync(Topic topic)
        {
            topic.CreatedAt = DateTime.UtcNow;
            topic.UpdatedAt = DateTime.UtcNow;
            await _context.Topics.AddAsync(topic);
            await _context.SaveChangesAsync();
            return topic;
        }

        public override async Task<Topic> UpdateAsync(Topic topic)
        {
            topic.UpdatedAt = DateTime.UtcNow;
            _context.Topics.Update(topic);
            await _context.SaveChangesAsync();
            return topic;
        }

        public async Task DeleteAsync(int id)
        {
            var topic = await GetByIdAsync(id);
            if (topic != null)
            {
                topic.IsDeleted = true;
                topic.UpdatedAt = DateTime.UtcNow;
                _context.Topics.Update(topic);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> CountByUserAsync(int userId)
        {
            return await _context.Topics
                .CountAsync(t => t.Followers.Any(f => f.Id == userId) && !t.IsDeleted);
        }

        public async Task<bool> IsFollowedAsync(int userId, int topicId)
        {
            return await _context.Topics
                .Include(t => t.Followers)
                .AnyAsync(t => t.Id == topicId && t.Followers.Any(f => f.Id == userId));
        }

        public override IQueryable<Topic> Query()
        {
            return _context.Topics
                .Include(t => t.Parent)
                .Include(t => t.Children)
                .Include(t => t.Posts)
                .Include(t => t.Followers)
                .Where(t => !t.IsDeleted);
        }

        public async Task<int> GetFollowerCountAsync(int topicId)
        {
            return await _context.Topics
                .Include(t => t.Followers)
                .Where(t => t.Id == topicId)
                .Select(t => t.Followers.Count)
                .FirstOrDefaultAsync();
        }
    }
} 