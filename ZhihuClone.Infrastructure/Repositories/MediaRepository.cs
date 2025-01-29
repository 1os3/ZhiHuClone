using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZhihuClone.Core.Interfaces;
using ZhihuClone.Core.Models.Content;
using ZhihuClone.Infrastructure.Data;

namespace ZhihuClone.Infrastructure.Repositories;

public class MediaRepository : IMediaRepository
{
    private readonly ApplicationDbContext _context;

    public MediaRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Media?> GetByIdAsync(int id)
    {
        return await _context.Media
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);
    }

    public async Task<List<Media>> GetAllAsync()
    {
        return await _context.Media
            .Where(m => !m.IsDeleted)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Media>> GetByPostIdAsync(int postId)
    {
        return await _context.Media
            .Where(m => m.Posts.Any(p => p.Id == postId) && !m.IsDeleted)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Media>> GetByUserIdAsync(int userId)
    {
        return await _context.Media
            .Where(m => m.CreatedByUserId == userId && !m.IsDeleted)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Media>> GetPagedAsync(int page = 1, int pageSize = 10)
    {
        return await _context.Media
            .Where(m => !m.IsDeleted)
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<List<Media>> SearchAsync(string searchTerm, int page = 1, int pageSize = 10)
    {
        var query = _context.Media.Where(m => !m.IsDeleted);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(m =>
                m.FileName.Contains(searchTerm) ||
                m.Description != null && m.Description.Contains(searchTerm));
        }

        return await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetTotalCountAsync(string? searchTerm = null)
    {
        var query = _context.Media.Where(m => !m.IsDeleted);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(m =>
                m.FileName.Contains(searchTerm) ||
                m.Description != null && m.Description.Contains(searchTerm));
        }

        return await query.CountAsync();
    }

    public async Task<bool> IsValidTypeAsync(string contentType)
    {
        var validTypes = new[] { "image/jpeg", "image/png", "image/gif", "video/mp4", "video/webm" };
        return validTypes.Contains(contentType.ToLower());
    }

    public async Task<string?> GetUrlAsync(int mediaId)
    {
        var media = await GetByIdAsync(mediaId);
        return media?.Url;
    }

    public async Task AssociateWithPostAsync(int mediaId, int postId)
    {
        var media = await GetByIdAsync(mediaId);
        var post = await _context.Posts.FindAsync(postId);

        if (media != null && post != null)
        {
            media.Posts.Add(post);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<Media?> SingleOrDefaultAsync(Expression<Func<Media, bool>> predicate)
    {
        return await _context.Media.SingleOrDefaultAsync(predicate);
    }

    public async Task<Media?> FirstOrDefaultAsync(Expression<Func<Media, bool>> predicate)
    {
        return await _context.Media.FirstOrDefaultAsync(predicate);
    }

    public async Task<bool> AnyAsync(Expression<Func<Media, bool>> predicate)
    {
        return await _context.Media.AnyAsync(predicate);
    }

    public async Task<int> CountAsync(Expression<Func<Media, bool>> predicate)
    {
        return await _context.Media.CountAsync(predicate);
    }

    public async Task<List<Media>> FindAsync(Expression<Func<Media, bool>> predicate)
    {
        return await _context.Media.Where(predicate).ToListAsync();
    }

    public async Task<Media> AddAsync(Media media)
    {
        await _context.Media.AddAsync(media);
        await _context.SaveChangesAsync();
        return media;
    }

    public async Task<IEnumerable<Media>> AddRangeAsync(IEnumerable<Media> entities)
    {
        await _context.Media.AddRangeAsync(entities);
        await _context.SaveChangesAsync();
        return entities;
    }

    public Media Update(Media media)
    {
        _context.Media.Update(media);
        _context.SaveChanges();
        return media;
    }

    public void Remove(Media media)
    {
        _context.Media.Remove(media);
        _context.SaveChanges();
    }

    public void RemoveRange(IEnumerable<Media> entities)
    {
        _context.Media.RemoveRange(entities);
        _context.SaveChanges();
    }

    public IQueryable<Media> Query()
    {
        return _context.Media;
    }

    public async Task RemoveAsync(Media media)
    {
        _context.Media.Remove(media);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveAsync(int id)
    {
        var media = await GetByIdAsync(id);
        if (media != null)
        {
            _context.Media.Remove(media);
            await _context.SaveChangesAsync();
        }
    }
} 