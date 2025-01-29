using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZhihuClone.Core.Interfaces.Security;
using ZhihuClone.Core.Models.Security;
using ZhihuClone.Infrastructure.Data;

namespace ZhihuClone.Infrastructure.Repositories.Security
{
    public class SpamPatternRepository : BaseRepository<SpamPattern>, ISpamPatternRepository
    {
        public SpamPatternRepository(ApplicationDbContext context) : base(context)
        {
        }

        public override async Task<SpamPattern?> GetByIdAsync(int id)
        {
            return await _context.Set<SpamPattern>().FindAsync(id);
        }

        public override async Task<List<SpamPattern>> GetAllAsync()
        {
            return await _context.Set<SpamPattern>()
                .Where(p => !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<SpamPattern>> GetPagedAsync(int page = 1, int pageSize = 20, string? pattern = null)
        {
            var query = _context.Set<SpamPattern>().Where(p => !p.IsDeleted);

            if (!string.IsNullOrEmpty(pattern))
            {
                query = query.Where(p => p.Pattern.Contains(pattern));
            }

            return await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<SpamPattern>> GetPagedWithFiltersAsync(int page = 1, int pageSize = 10, string? pattern = null, string? category = null, bool? isEnabled = null)
        {
            var query = _context.Set<SpamPattern>().Where(p => !p.IsDeleted);

            if (!string.IsNullOrEmpty(pattern))
            {
                query = query.Where(p => p.Pattern.Contains(pattern));
            }

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(p => p.Category == category);
            }

            if (isEnabled.HasValue)
            {
                query = query.Where(p => p.IsEnabled == isEnabled.Value);
            }

            return await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public override async Task<SpamPattern> AddAsync(SpamPattern pattern)
        {
            pattern.CreatedAt = DateTime.UtcNow;
            await _context.Set<SpamPattern>().AddAsync(pattern);
            return pattern;
        }

        public override async Task<SpamPattern> UpdateAsync(SpamPattern pattern)
        {
            pattern.UpdatedAt = DateTime.UtcNow;
            _context.Entry(pattern).State = EntityState.Modified;
            return pattern;
        }

        public override async Task RemoveAsync(int id)
        {
            var pattern = await GetByIdAsync(id);
            if (pattern != null)
            {
                pattern.IsDeleted = true;
                pattern.UpdatedAt = DateTime.UtcNow;
                _context.Entry(pattern).State = EntityState.Modified;
            }
        }

        public async Task<bool> IsPatternExistsAsync(string pattern)
        {
            return await _context.Set<SpamPattern>()
                .AnyAsync(p => p.Pattern == pattern && !p.IsDeleted);
        }

        public async Task<int> CountAsync(string? pattern = null, string? category = null, bool? isEnabled = null)
        {
            var query = _context.Set<SpamPattern>().Where(p => !p.IsDeleted);

            if (!string.IsNullOrEmpty(pattern))
            {
                query = query.Where(p => p.Pattern.Contains(pattern));
            }

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(p => p.Category == category);
            }

            if (isEnabled.HasValue)
            {
                query = query.Where(p => p.IsEnabled == isEnabled.Value);
            }

            return await query.CountAsync();
        }

        public async Task<int> GetTotalCountAsync()
        {
            return await _context.Set<SpamPattern>()
                .Where(p => !p.IsDeleted)
                .CountAsync();
        }

        public async Task BulkInsertAsync(IEnumerable<SpamPattern> patterns)
        {
            foreach (var pattern in patterns)
            {
                pattern.CreatedAt = DateTime.UtcNow;
            }
            await _context.Set<SpamPattern>().AddRangeAsync(patterns);
        }

        public async Task BulkUpdateAsync(IEnumerable<SpamPattern> patterns)
        {
            foreach (var pattern in patterns)
            {
                pattern.UpdatedAt = DateTime.UtcNow;
                _context.Entry(pattern).State = EntityState.Modified;
            }
        }

        public async Task<List<string>> GetCategoriesAsync()
        {
            return await _context.Set<SpamPattern>()
                .Where(p => !p.IsDeleted && !string.IsNullOrEmpty(p.Category))
                .Select(p => p.Category)
                .Distinct()
                .ToListAsync();
        }

        public async Task<SpamPattern?> GetByPatternAsync(string pattern)
        {
            return await _context.Set<SpamPattern>()
                .FirstOrDefaultAsync(p => p.Pattern == pattern && !p.IsDeleted);
        }

        public async Task<List<SpamPattern>> GetByCategoryAsync(string category)
        {
            return await _context.Set<SpamPattern>()
                .Where(p => p.Category == category && !p.IsDeleted)
                .ToListAsync();
        }

        public async Task<List<SpamPattern>> GetEnabledAsync()
        {
            return await _context.Set<SpamPattern>()
                .Where(p => p.IsEnabled && !p.IsDeleted)
                .ToListAsync();
        }

        public async Task<List<SpamPattern>> GetAllEnabledAsync()
        {
            return await GetEnabledAsync();
        }

        public async Task<List<SpamPattern>> GetRegexPatternsAsync()
        {
            return await _context.Set<SpamPattern>()
                .Where(p => p.IsEnabled && !p.IsDeleted && p.IsRegex)
                .ToListAsync();
        }

        public async Task<bool> IsSpamAsync(string content)
        {
            var patterns = await GetEnabledAsync();
            foreach (var pattern in patterns)
            {
                if (pattern.IsRegex)
                {
                    if (Regex.IsMatch(content, pattern.Pattern))
                        return true;
                }
                else
                {
                    if (content.Contains(pattern.Pattern))
                        return true;
                }
            }
            return false;
        }

        public async Task<List<string>> FindSpamPatternsAsync(string content)
        {
            var patterns = await GetEnabledAsync();
            var matches = new List<string>();

            foreach (var pattern in patterns)
            {
                if (pattern.IsRegex)
                {
                    if (Regex.IsMatch(content, pattern.Pattern))
                        matches.Add(pattern.Pattern);
                }
                else
                {
                    if (content.Contains(pattern.Pattern))
                        matches.Add(pattern.Pattern);
                }
            }

            return matches;
        }

        public async Task<Dictionary<string, List<string>>> AnalyzeContentAsync(string content)
        {
            var patterns = await GetEnabledAsync();
            var result = new Dictionary<string, List<string>>();

            foreach (var pattern in patterns)
            {
                var matches = new List<string>();
                if (pattern.IsRegex)
                {
                    var regexMatches = Regex.Matches(content, pattern.Pattern);
                    matches.AddRange(regexMatches.Select(m => m.Value));
                }
                else
                {
                    var index = content.IndexOf(pattern.Pattern);
                    while (index != -1)
                    {
                        matches.Add(pattern.Pattern);
                        index = content.IndexOf(pattern.Pattern, index + 1);
                    }
                }

                if (matches.Any())
                {
                    result[pattern.Pattern] = matches;
                }
            }

            return result;
        }

        public async Task IncrementMatchCountAsync(int patternId)
        {
            var pattern = await GetByIdAsync(patternId);
            if (pattern != null)
            {
                pattern.MatchCount++;
                pattern.LastMatchAt = DateTime.UtcNow;
                _context.Entry(pattern).State = EntityState.Modified;
            }
        }

        public async Task UpdateLastMatchTimeAsync(int patternId)
        {
            var pattern = await GetByIdAsync(patternId);
            if (pattern != null)
            {
                pattern.LastMatchAt = DateTime.UtcNow;
                _context.Entry(pattern).State = EntityState.Modified;
            }
        }

        public async Task<List<SpamPattern>> GetTopMatchedPatternsAsync(int count)
        {
            return await _context.Set<SpamPattern>()
                .Where(p => !p.IsDeleted)
                .OrderByDescending(p => p.MatchCount)
                .Take(count)
                .ToListAsync();
        }

        public async Task<SpamPattern?> GetByTypeAsync(string type)
        {
            return await _context.Set<SpamPattern>()
                .FirstOrDefaultAsync(p => p.Type == type && !p.IsDeleted);
        }

        public async Task DeleteAsync(int id)
        {
            await RemoveAsync(id);
        }
    }
} 