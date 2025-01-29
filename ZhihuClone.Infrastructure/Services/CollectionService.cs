using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZhihuClone.Core.Interfaces;
using ZhihuClone.Core.Models;
using ZhihuClone.Infrastructure.Data;

namespace ZhihuClone.Infrastructure.Services
{
    public class CollectionService : ICollectionService
    {
        private readonly ApplicationDbContext _context;

        public CollectionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Collection> GetByIdAsync(int id)
        {
            return await _context.Collections
                .Include(c => c.Posts)
                .Include(c => c.Followers)
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
        }

        public async Task<List<Collection>> GetUserCollectionsAsync(int userId, int page = 1, int pageSize = 10)
        {
            return await _context.Collections
                .Include(c => c.Posts)
                .Include(c => c.Followers)
                .Where(c => c.CreatedByUserId == userId && !c.IsDeleted)
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<Collection> CreateAsync(Collection collection)
        {
            collection.CreatedAt = DateTime.UtcNow;
            _context.Collections.Add(collection);
            await _context.SaveChangesAsync();
            return collection;
        }

        public async Task<Collection> UpdateAsync(Collection collection)
        {
            collection.UpdatedAt = DateTime.UtcNow;
            _context.Collections.Update(collection);
            await _context.SaveChangesAsync();
            return collection;
        }

        public async Task DeleteAsync(int id)
        {
            var collection = await _context.Collections.FindAsync(id);
            if (collection != null)
            {
                collection.IsDeleted = true;
                collection.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> IsFollowingAsync(int userId, int collectionId)
        {
            return await _context.Collections
                .Include(c => c.Followers)
                .AnyAsync(c => c.Id == collectionId && c.Followers.Any(f => f.Id == userId));
        }

        public async Task FollowAsync(int userId, int collectionId)
        {
            var collection = await _context.Collections
                .Include(c => c.Followers)
                .FirstOrDefaultAsync(c => c.Id == collectionId);

            if (collection != null)
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null && !collection.Followers.Any(f => f.Id == userId))
                {
                    collection.Followers.Add(user);
                    collection.FollowerCount++;
                    await _context.SaveChangesAsync();
                }
            }
        }

        public async Task UnfollowAsync(int userId, int collectionId)
        {
            var collection = await _context.Collections
                .Include(c => c.Followers)
                .FirstOrDefaultAsync(c => c.Id == collectionId);

            if (collection != null)
            {
                var user = collection.Followers.FirstOrDefault(f => f.Id == userId);
                if (user != null)
                {
                    collection.Followers.Remove(user);
                    collection.FollowerCount--;
                    await _context.SaveChangesAsync();
                }
            }
        }
    }
} 