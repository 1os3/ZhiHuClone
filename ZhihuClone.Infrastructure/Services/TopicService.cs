using Microsoft.EntityFrameworkCore;
using ZhihuClone.Core.Interfaces;
using ZhihuClone.Core.Models.Content;
using ZhihuClone.Infrastructure.Data;

namespace ZhihuClone.Infrastructure.Services
{
    public class TopicService : ITopicService
    {
        private readonly ApplicationDbContext _context;

        public TopicService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Topic?> GetByIdAsync(int id)
        {
            return await _context.Topics
                .Include(t => t.Posts)
                .Include(t => t.Followers)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<List<Topic>> GetAllAsync()
        {
            return await _context.Topics
                .Include(t => t.Posts)
                .Include(t => t.Followers)
                .ToListAsync();
        }

        public async Task<List<Topic>> GetHotTopicsAsync(int count)
        {
            return await _context.Topics
                .Include(t => t.Posts)
                .Include(t => t.Followers)
                .OrderByDescending(t => t.Followers.Count)
                .ThenByDescending(t => t.Posts.Count)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<Topic>> GetByUserIdAsync(int userId)
        {
            return await _context.Topics
                .Include(t => t.Followers)
                .Where(t => t.Followers.Any(u => u.Id == userId))
                .ToListAsync();
        }

        public async Task<List<Topic>> GetByPostIdAsync(int postId)
        {
            return await _context.Topics
                .Include(t => t.Posts)
                .Where(t => t.Posts.Any(p => p.Id == postId))
                .ToListAsync();
        }

        public async Task<List<Topic>> GetByIdsAsync(IEnumerable<int> ids)
        {
            if (ids == null || !ids.Any())
            {
                return new List<Topic>();
            }

            return await _context.Topics
                .Include(t => t.Posts)
                .Include(t => t.Followers)
                .Where(t => ids.Contains(t.Id))
                .ToListAsync();
        }

        public async Task<List<Topic>> SearchAsync(string searchTerm, int page = 1, int pageSize = 10)
        {
            var query = _context.Topics.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(t => 
                    t.Name.Contains(searchTerm) || 
                    t.Description.Contains(searchTerm));
            }

            return await query
                .OrderByDescending(t => t.Followers.Count)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetTotalCountAsync(string? searchTerm = null)
        {
            var query = _context.Topics.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(t => 
                    t.Name.Contains(searchTerm) || 
                    t.Description.Contains(searchTerm));
            }

            return await query.CountAsync();
        }

        public async Task<Topic> CreateAsync(Topic topic)
        {
            await _context.Topics.AddAsync(topic);
            await _context.SaveChangesAsync();
            return topic;
        }

        public async Task UpdateAsync(Topic topic)
        {
            _context.Topics.Update(topic);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var topic = await _context.Topics.FindAsync(id);
            if (topic != null)
            {
                _context.Topics.Remove(topic);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> FollowAsync(int userId, int topicId)
        {
            var user = await _context.Users.FindAsync(userId);
            var topic = await _context.Topics.FindAsync(topicId);

            if (user == null || topic == null)
            {
                return false;
            }

            var isFollowing = await _context.Topics
                .Where(t => t.Id == topicId)
                .SelectMany(t => t.Followers)
                .AnyAsync(u => u.Id == userId);

            if (isFollowing)
            {
                return false;
            }

            topic.Followers.Add(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UnfollowAsync(int userId, int topicId)
        {
            var user = await _context.Users.FindAsync(userId);
            var topic = await _context.Topics.FindAsync(topicId);

            if (user == null || topic == null)
            {
                return false;
            }

            var isFollowing = await _context.Topics
                .Where(t => t.Id == topicId)
                .SelectMany(t => t.Followers)
                .AnyAsync(u => u.Id == userId);

            if (!isFollowing)
            {
                return false;
            }

            topic.Followers.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsFollowingAsync(int userId, int topicId)
        {
            return await _context.Topics
                .Where(t => t.Id == topicId)
                .SelectMany(t => t.Followers)
                .AnyAsync(u => u.Id == userId);
        }

        public async Task<int> GetFollowerCountAsync(int topicId)
        {
            return await _context.Topics
                .Where(t => t.Id == topicId)
                .SelectMany(t => t.Followers)
                .CountAsync();
        }
    }
} 