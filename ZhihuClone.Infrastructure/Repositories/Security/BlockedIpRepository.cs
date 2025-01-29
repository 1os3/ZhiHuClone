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
    public class BlockedIpRepository : BaseRepository<BlockedIp>, IBlockedIpRepository
    {
        public BlockedIpRepository(ApplicationDbContext context) : base(context)
        {
        }

        public override async Task<BlockedIp?> GetByIdAsync(int id)
        {
            return await _context.Set<BlockedIp>().FindAsync(id);
        }

        public async Task<List<BlockedIp>> GetAllAsync(string? ipAddress = null, string? status = null)
        {
            var query = _context.Set<BlockedIp>().AsQueryable();

            if (!string.IsNullOrEmpty(ipAddress))
            {
                query = query.Where(b => b.IpAddress.Contains(ipAddress));
            }

            if (!string.IsNullOrEmpty(status))
            {
                bool isEnabled = status.ToLower() == "active";
                query = query.Where(b => b.IsEnabled == isEnabled);
            }

            return await query.ToListAsync();
        }

        public async Task<List<BlockedIp>> GetPagedAsync(int page = 1, int pageSize = 20, string? ipAddress = null, string? status = null, string? reason = null)
        {
            var query = _context.Set<BlockedIp>().AsQueryable();

            if (!string.IsNullOrEmpty(ipAddress))
            {
                query = query.Where(b => b.IpAddress.Contains(ipAddress));
            }

            if (!string.IsNullOrEmpty(status))
            {
                bool isEnabled = status.ToLower() == "active";
                query = query.Where(b => b.IsEnabled == isEnabled);
            }

            if (!string.IsNullOrEmpty(reason))
            {
                query = query.Where(b => b.Reason.Contains(reason));
            }

            return await query
                .OrderByDescending(b => b.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public override async Task<BlockedIp> AddAsync(BlockedIp blockedIp)
        {
            blockedIp.CreatedAt = DateTime.UtcNow;
            await _context.Set<BlockedIp>().AddAsync(blockedIp);
            return blockedIp;
        }

        public override async Task<BlockedIp> UpdateAsync(BlockedIp blockedIp)
        {
            blockedIp.UpdatedAt = DateTime.UtcNow;
            _context.Entry(blockedIp).State = EntityState.Modified;
            return blockedIp;
        }

        public async Task DeleteAsync(int id)
        {
            var blockedIp = await GetByIdAsync(id);
            if (blockedIp != null)
            {
                blockedIp.IsEnabled = false;
                blockedIp.UnblockedAt = DateTime.UtcNow;
                await UpdateAsync(blockedIp);
            }
        }

        public async Task<bool> IsBlockedAsync(string ipAddress)
        {
            var now = DateTime.UtcNow;
            return await _context.Set<BlockedIp>()
                .AnyAsync(b => b.IpAddress == ipAddress && 
                              b.IsEnabled && 
                              (!b.UnblockedAt.HasValue || b.UnblockedAt > now));
        }

        public async Task<int> GetActiveBlockCountAsync()
        {
            var now = DateTime.UtcNow;
            return await _context.Set<BlockedIp>()
                .CountAsync(b => b.IsEnabled && (!b.UnblockedAt.HasValue || b.UnblockedAt > now));
        }

        public async Task<List<BlockedIp>> GetActiveBlocksAsync(int page = 1, int pageSize = 20)
        {
            var now = DateTime.UtcNow;
            return await _context.Set<BlockedIp>()
                .Where(b => b.IsEnabled && (!b.UnblockedAt.HasValue || b.UnblockedAt > now))
                .OrderByDescending(b => b.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> CountAsync(string? ipAddress = null, string? status = null, string? reason = null)
        {
            var query = _context.Set<BlockedIp>().AsQueryable();

            if (!string.IsNullOrEmpty(ipAddress))
            {
                query = query.Where(b => b.IpAddress.Contains(ipAddress));
            }

            if (!string.IsNullOrEmpty(status))
            {
                bool isEnabled = status.ToLower() == "active";
                query = query.Where(b => b.IsEnabled == isEnabled);
            }

            if (!string.IsNullOrEmpty(reason))
            {
                query = query.Where(b => b.Reason.Contains(reason));
            }

            return await query.CountAsync();
        }

        public async Task<BlockedIp?> GetByIpAsync(string ipAddress)
        {
            return await _context.Set<BlockedIp>()
                .FirstOrDefaultAsync(b => b.IpAddress == ipAddress);
        }

        public async Task<List<BlockedIp>> GetHistoryAsync(string ipAddress, int page = 1, int pageSize = 20)
        {
            return await _context.Set<BlockedIp>()
                .Where(b => b.IpAddress == ipAddress)
                .OrderByDescending(b => b.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
    }
} 