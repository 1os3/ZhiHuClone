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
    public class AccessLogRepository : Repository<AccessLog>, IAccessLogRepository
    {
        public AccessLogRepository(DbContext context) : base(context)
        {
        }

        public async Task<List<AccessLog>> GetByActionAsync(string action)
        {
            return await _dbSet
                .Where(x => x.Action == action)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<AccessLog>> GetByIpAsync(string ipAddress)
        {
            return await _dbSet
                .Where(x => x.IpAddress == ipAddress)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<AccessLog>> GetByUserIdAsync(int userId)
        {
            return await _dbSet
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<AccessLog>> GetFailedAttemptsAsync(DateTime startTime, DateTime endTime)
        {
            return await _dbSet
                .Where(x => !x.IsSuccess && x.CreatedAt >= startTime && x.CreatedAt <= endTime)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> CountAsync(string? action = null, string? ipAddress = null, int? userId = null, bool? isSuccess = null)
        {
            var query = _dbSet.AsQueryable();

            if (!string.IsNullOrEmpty(action))
                query = query.Where(x => x.Action == action);

            if (!string.IsNullOrEmpty(ipAddress))
                query = query.Where(x => x.IpAddress == ipAddress);

            if (userId.HasValue)
                query = query.Where(x => x.UserId == userId.Value);

            if (isSuccess.HasValue)
                query = query.Where(x => x.IsSuccess == isSuccess.Value);

            return await query.CountAsync();
        }

        public async Task<List<AccessLog>> GetPagedAsync(int page, int pageSize, string? action = null, string? ipAddress = null, int? userId = null, bool? isSuccess = null)
        {
            var query = _dbSet.AsQueryable();

            if (!string.IsNullOrEmpty(action))
                query = query.Where(x => x.Action == action);

            if (!string.IsNullOrEmpty(ipAddress))
                query = query.Where(x => x.IpAddress == ipAddress);

            if (userId.HasValue)
                query = query.Where(x => x.UserId == userId.Value);

            if (isSuccess.HasValue)
                query = query.Where(x => x.IsSuccess == isSuccess.Value);

            return await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<Dictionary<string, int>> GetActionStatisticsAsync(DateTime startTime, DateTime endTime)
        {
            return await _dbSet
                .Where(x => x.CreatedAt >= startTime && x.CreatedAt <= endTime)
                .GroupBy(x => x.Action)
                .Select(g => new { Action = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Action, x => x.Count);
        }

        public async Task<Dictionary<string, int>> GetIpStatisticsAsync(DateTime startTime, DateTime endTime)
        {
            return await _dbSet
                .Where(x => x.CreatedAt >= startTime && x.CreatedAt <= endTime)
                .GroupBy(x => x.IpAddress)
                .Select(g => new { IpAddress = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.IpAddress, x => x.Count);
        }

        public async Task<Dictionary<string, int>> GetUserStatisticsAsync(DateTime startTime, DateTime endTime)
        {
#pragma warning disable CS8629 // Nullable value type may be null.
            return await _dbSet
                .Where(x => x.CreatedAt >= startTime && x.CreatedAt <= endTime && x.UserId != null)
                .GroupBy(x => x.UserId.Value.ToString())
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.UserId, x => x.Count);
#pragma warning restore CS8629 // Nullable value type may be null.
        }

        public async Task<IEnumerable<AccessLog>> GetRecentAccessesByIpAsync(string ipAddress)
        {
            var oneMinuteAgo = DateTime.UtcNow.AddMinutes(-1);
            return await _dbSet
                .Where(x => x.IpAddress == ipAddress && x.CreatedAt >= oneMinuteAgo)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<AccessLog>> GetRecentFailuresByIpAsync(string ipAddress)
        {
            var oneHourAgo = DateTime.UtcNow.AddHours(-1);
            return await _dbSet
                .Where(x => x.IpAddress == ipAddress && !x.IsSuccess && x.CreatedAt >= oneHourAgo)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> GetFailureCountAsync(string ipAddress, TimeSpan duration)
        {
            var startTime = DateTime.UtcNow.Subtract(duration);
            return await _dbSet
                .CountAsync(x => x.IpAddress == ipAddress && !x.IsSuccess && x.CreatedAt >= startTime);
        }

        public async Task<Dictionary<string, int>> GetTopFailedIpsAsync(int count)
        {
            var oneHourAgo = DateTime.UtcNow.AddHours(-1);
            return await _dbSet
                .Where(x => !x.IsSuccess && x.CreatedAt >= oneHourAgo)
                .GroupBy(x => x.IpAddress)
                .Select(g => new { IpAddress = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(count)
                .ToDictionaryAsync(x => x.IpAddress, x => x.Count);
        }
    }
} 