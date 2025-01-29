using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZhihuClone.Core.Interfaces.Security;
using ZhihuClone.Core.Models.Security;
using ZhihuClone.Infrastructure.Data;

namespace ZhihuClone.Infrastructure.Repositories.Security
{
    public class FileSignatureRepository : BaseRepository<FileSignature>, IFileSignatureRepository
    {
        private new readonly ApplicationDbContext _context;

        public FileSignatureRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<string>> GetFileTypesAsync()
        {
            return await _context.FileSignatures
                .Select(f => f.FileType)
                .Distinct()
                .ToListAsync();
        }

        public async Task<FileSignature?> GetBySignatureAsync(string signature)
        {
            return await _context.FileSignatures
                .FirstOrDefaultAsync(f => f.Signature == signature);
        }

        public async Task<List<FileSignature>> GetByFileTypeAsync(string fileType)
        {
            return await _context.FileSignatures
                .Where(f => f.FileType == fileType)
                .ToListAsync();
        }

        public async Task<List<FileSignature>> GetWhitelistedAsync()
        {
            return await _context.FileSignatures
                .Where(f => f.IsEnabled && !f.IsBlocked)
                .ToListAsync();
        }

        public async Task<List<FileSignature>> GetPagedAsync(int page = 1, int pageSize = 10, string? signature = null, string? fileType = null, bool? isWhitelisted = null)
        {
            var query = _context.FileSignatures.AsQueryable();

            if (!string.IsNullOrEmpty(signature))
                query = query.Where(f => f.Hash.Contains(signature));

            if (!string.IsNullOrEmpty(fileType))
                query = query.Where(f => f.ContentType == fileType);

            if (isWhitelisted.HasValue)
                query = query.Where(f => f.IsEnabled && !f.IsDeleted == isWhitelisted.Value);

            return await query
                .OrderByDescending(f => f.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> CountAsync(string? signature = null, string? fileType = null, bool? isWhitelisted = null)
        {
            var query = _context.FileSignatures.AsQueryable();

            if (!string.IsNullOrEmpty(signature))
                query = query.Where(f => f.Signature.Contains(signature));

            if (!string.IsNullOrEmpty(fileType))
                query = query.Where(f => f.FileType == fileType);

            if (isWhitelisted.HasValue)
                query = query.Where(f => f.IsEnabled && !f.IsBlocked == isWhitelisted.Value);

            return await query.CountAsync();
        }

        public async Task BulkInsertAsync(IEnumerable<FileSignature> signatures)
        {
            await _context.FileSignatures.AddRangeAsync(signatures);
            await _context.SaveChangesAsync();
        }

        public async Task BulkUpdateAsync(IEnumerable<FileSignature> signatures)
        {
            _context.FileSignatures.UpdateRange(signatures);
            await _context.SaveChangesAsync();
        }

        public async Task IncrementUseCountAsync(string signature)
        {
            var fileSignature = await GetBySignatureAsync(signature);
            if (fileSignature != null)
            {
                fileSignature.DetectionCount++;
                fileSignature.LastDetectedAt = DateTime.UtcNow;
                _context.FileSignatures.Update(fileSignature);
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdateLastUsedTimeAsync(string signature)
        {
            var fileSignature = await GetBySignatureAsync(signature);
            if (fileSignature != null)
            {
                fileSignature.LastDetectedAt = DateTime.UtcNow;
                _context.FileSignatures.Update(fileSignature);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<FileSignature>> GetMostUsedSignaturesAsync(int count)
        {
            return await _context.FileSignatures
                .OrderByDescending(f => f.DetectionCount)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<FileSignature>> GetAllAsync(int page = 1, int pageSize = 20)
        {
            return await _context.FileSignatures
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public new async Task<FileSignature> AddAsync(FileSignature entity)
        {
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;
            await _context.FileSignatures.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public new async Task<FileSignature> UpdateAsync(FileSignature entity)
        {
            entity.UpdatedAt = DateTime.UtcNow;
            _context.FileSignatures.Update(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var fileSignature = await _context.FileSignatures.FindAsync(id);
            if (fileSignature == null)
                return false;

            _context.FileSignatures.Remove(fileSignature);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<FileSignature?> GetByHashAsync(string hash)
        {
            return await _context.FileSignatures
                .FirstOrDefaultAsync(f => f.Hash == hash && !f.IsDeleted);
        }

        public async Task<List<FileSignature>> GetByUserIdAsync(int userId)
        {
            return await _context.FileSignatures
                .Where(f => f.CreatedByUserId == userId && !f.IsDeleted)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> GetTotalCountAsync()
        {
            return await _context.FileSignatures
                .Where(f => !f.IsDeleted)
                .CountAsync();
        }

        public async Task<bool> ExistsAsync(string hash)
        {
            return await _context.FileSignatures
                .AnyAsync(f => f.Hash == hash && !f.IsDeleted);
        }
    }
} 