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
    public class SensitiveWordRepository : BaseRepository<SensitiveWord>, ISensitiveWordRepository
    {
        private new readonly ApplicationDbContext _context;

        public SensitiveWordRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<string>> GetCategoriesAsync()
        {
            return await _dbSet
                .Select(x => x.Category)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();
        }

        public async Task<SensitiveWord?> GetByWordAsync(string word)
        {
            return await _context.SensitiveWords
                .FirstOrDefaultAsync(x => x.Word == word);
        }

        public async Task<List<SensitiveWord>> GetAllAsync(int page = 1, int pageSize = 20)
        {
            return await _context.SensitiveWords
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public new async Task<SensitiveWord> AddAsync(SensitiveWord entity)
        {
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SensitiveWords.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public new async Task<SensitiveWord> UpdateAsync(SensitiveWord entity)
        {
            entity.UpdatedAt = DateTime.UtcNow;
            _context.SensitiveWords.Update(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var sensitiveWord = await _context.SensitiveWords.FindAsync(id);
            if (sensitiveWord == null)
                return false;

            _context.SensitiveWords.Remove(sensitiveWord);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<SensitiveWord>> GetByCategoryAsync(string category)
        {
            return await _dbSet
                .Where(x => x.Category == category)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<SensitiveWord>> GetByLevelAsync(int level)
        {
            return await _dbSet
                .Where(x => x.Level == level)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<SensitiveWord>> GetEnabledAsync()
        {
            return await _dbSet
                .Where(x => x.IsEnabled)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<SensitiveWord>> GetPagedAsync(int page, int pageSize, string? word = null, string? category = null, int? level = null)
        {
            var query = _dbSet.AsQueryable();

            if (!string.IsNullOrEmpty(word))
                query = query.Where(x => x.Word.Contains(word));

            if (!string.IsNullOrEmpty(category))
                query = query.Where(x => x.Category == category);

            if (level.HasValue)
                query = query.Where(x => x.Level == level.Value);

            return await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> CountAsync(string? word = null, string? category = null, int? level = null)
        {
            var query = _dbSet.AsQueryable();

            if (!string.IsNullOrEmpty(word))
                query = query.Where(x => x.Word.Contains(word));

            if (!string.IsNullOrEmpty(category))
                query = query.Where(x => x.Category == category);

            if (level.HasValue)
                query = query.Where(x => x.Level == level.Value);

            return await query.CountAsync();
        }

        public async Task<bool> IsWordSensitiveAsync(string word)
        {
            if (string.IsNullOrEmpty(word))
                return false;

            // 检查普通敏感词
            var isNormalMatch = await _dbSet
                .AnyAsync(x => x.IsEnabled && !x.IsRegex && 
                              word.Contains(x.Word, StringComparison.OrdinalIgnoreCase));
            if (isNormalMatch)
                return true;

            // 检查正则表达式
            var regexPatterns = await GetRegexPatternsAsync();
            foreach (var pattern in regexPatterns)
            {
                try
                {
                    if (Regex.IsMatch(word, pattern.Word, RegexOptions.IgnoreCase))
                        return true;
                }
                catch
                {
                    // 忽略无效的正则表达式
                    continue;
                }
            }

            return false;
        }

        public async Task<IEnumerable<string>> FindSensitiveWordsAsync(string content)
        {
            if (string.IsNullOrEmpty(content))
                return Enumerable.Empty<string>();

            var result = new HashSet<string>();
            var sensitiveWords = await GetAllEnabledAsync();

            // 检查普通敏感词
            foreach (var word in sensitiveWords.Where(x => !x.IsRegex))
            {
                if (content.Contains(word.Word, StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(word.Word);
                }
            }

            // 检查正则表达式
            foreach (var pattern in sensitiveWords.Where(x => x.IsRegex))
            {
                try
                {
                    var matches = Regex.Matches(content, pattern.Word, RegexOptions.IgnoreCase);
                    foreach (Match match in matches)
                    {
                        result.Add(match.Value);
                    }
                }
                catch
                {
                    // 忽略无效的正则表达式
                    continue;
                }
            }

            return result;
        }

        public async Task<bool> ContainsSensitiveWordsAsync(string content)
        {
            if (string.IsNullOrEmpty(content))
                return false;

            var sensitiveWords = await GetAllEnabledAsync();

            // 检查普通敏感词
            foreach (var word in sensitiveWords.Where(x => !x.IsRegex))
            {
                if (content.Contains(word.Word, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            // 检查正则表达式
            foreach (var pattern in sensitiveWords.Where(x => x.IsRegex))
            {
                try
                {
                    if (Regex.IsMatch(content, pattern.Word, RegexOptions.IgnoreCase))
                        return true;
                }
                catch
                {
                    // 忽略无效的正则表达式
                    continue;
                }
            }

            return false;
        }

        public async Task<Dictionary<string, List<string>>> AnalyzeContentAsync(string content)
        {
            var result = new Dictionary<string, List<string>>();
            if (string.IsNullOrEmpty(content))
                return result;

            var sensitiveWords = await GetAllEnabledAsync();
            var categories = sensitiveWords.Select(x => x.Category).Distinct();

            foreach (var category in categories)
            {
                var categoryWords = sensitiveWords.Where(x => x.Category == category);
                var matches = new List<string>();

                // 检查普通敏感词
                foreach (var word in categoryWords.Where(x => !x.IsRegex))
                {
                    if (content.Contains(word.Word, StringComparison.OrdinalIgnoreCase))
                    {
                        matches.Add(word.Word);
                    }
                }

                // 检查正则表达式
                foreach (var pattern in categoryWords.Where(x => x.IsRegex))
                {
                    try
                    {
                        var regexMatches = Regex.Matches(content, pattern.Word, RegexOptions.IgnoreCase)
                            .Select(m => m.Value)
                            .Distinct();
                        matches.AddRange(regexMatches);
                    }
                    catch
                    {
                        // 忽略无效的正则表达式
                        continue;
                    }
                }

                if (matches.Any())
                {
                    result[category] = matches.Distinct().ToList();
                }
            }

            return result;
        }

        public async Task BulkInsertAsync(IEnumerable<SensitiveWord> words)
        {
            await _dbSet.AddRangeAsync(words);
            await _context.SaveChangesAsync();
        }

        public async Task BulkUpdateAsync(IEnumerable<SensitiveWord> words)
        {
            _dbSet.UpdateRange(words);
            await _context.SaveChangesAsync();
        }

        public async Task<List<SensitiveWord>> GetAllEnabledAsync()
        {
            return await _dbSet.Where(x => x.IsEnabled).ToListAsync();
        }

        public async Task<List<SensitiveWord>> GetRegexPatternsAsync()
        {
            return await _dbSet
                .Where(x => x.IsEnabled && x.IsRegex)
                .ToListAsync();
        }

        public async Task<bool> ContainsAnyAsync(string content)
        {
            var patterns = await GetRegexPatternsAsync();
            if (patterns.Count == 0)
                return false;

            var combinedPattern = string.Join("|", patterns);
            return Regex.IsMatch(content, combinedPattern, RegexOptions.IgnoreCase);
        }

        public async Task<List<string>> FindMatchesAsync(string content)
        {
            var patterns = await GetRegexPatternsAsync();
            if (patterns.Count == 0)
                return new List<string>();

            var combinedPattern = string.Join("|", patterns);
            var matches = Regex.Matches(content, combinedPattern, RegexOptions.IgnoreCase);
            return matches.Select(m => m.Value).Distinct().ToList();
        }

        public async Task<(bool isClean, List<string> matches)> ValidateContentAsync(string content)
        {
            var matches = await FindMatchesAsync(content);
            return (matches.Count == 0, matches);
        }

        public async Task<int> GetTotalCountAsync()
        {
            return await _context.SensitiveWords
                .Where(w => !w.IsDeleted)
                .CountAsync();
        }

        public async Task<bool> ExistsAsync(string word)
        {
            return await _context.SensitiveWords
                .AnyAsync(w => w.Word == word && !w.IsDeleted);
        }
    }
} 