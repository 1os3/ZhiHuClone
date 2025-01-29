using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TinyPinyin;
using ZhihuClone.Core.Interfaces;
using ZhihuClone.Core.Models.Search;
using ZhihuClone.Infrastructure.Data;

namespace ZhihuClone.Infrastructure.Services
{
    public class SearchExtensionService : ISearchExtensionService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICacheService _cacheService;
        private static readonly TimeSpan CacheExpiry = TimeSpan.FromHours(1);

        public SearchExtensionService(ApplicationDbContext context, ICacheService cacheService)
        {
            _context = context;
            _cacheService = cacheService;
        }

        public async Task<string> GetSearchTextAsync(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            var cacheKey = $"search_text:{text}";
            var cachedText = await _cacheService.GetAsync<string>(cacheKey);
            if (!string.IsNullOrEmpty(cachedText))
                return cachedText;

            var expandedText = await ExpandKeywordsAsync(text);
            await _cacheService.SetAsync(cacheKey, expandedText, CacheExpiry);
            return expandedText;
        }

        public async Task<string> ExpandKeywordsAsync(string keyword)
        {
            if (string.IsNullOrEmpty(keyword))
                return keyword;

            var words = keyword.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var expandedWords = new List<string>();

            foreach (var word in words)
            {
                // 添加原词
                expandedWords.Add(word);

                // 添加拼音
                var pinyin = PinyinHelper.GetPinyin(word);
                if (pinyin != word)
                    expandedWords.Add(pinyin);

                // 添加同义词
                var synonyms = await GetSynonymsAsync(word);
                expandedWords.AddRange(synonyms);
            }

            return string.Join(" OR ", expandedWords.Distinct());
        }

        public async Task<string[]> GetSynonymsAsync(string word)
        {
            if (string.IsNullOrEmpty(word))
                return Array.Empty<string>();

            var cacheKey = $"synonyms:{word}";
            var cachedSynonyms = await _cacheService.GetAsync<string[]>(cacheKey);
            if (cachedSynonyms != null)
                return cachedSynonyms;

            var synonyms = await _context.Synonyms
                .Where(s => (s.Word == word || s.SynonymWord == word) && s.IsEnabled && !s.IsDeleted)
                .OrderByDescending(s => s.Priority)
                .Select(s => s.Word == word ? s.SynonymWord : s.Word)
                .ToArrayAsync();

            await _cacheService.SetAsync(cacheKey, synonyms, CacheExpiry);
            return synonyms;
        }

        public async Task AddSynonymAsync(string word1, string word2, float weight = 1.0f)
        {
            var existingSynonym = await _context.Synonyms
                .FirstOrDefaultAsync(s =>
                    (s.Word == word1 && s.SynonymWord == word2) ||
                    (s.Word == word2 && s.SynonymWord == word1));

            if (existingSynonym == null)
            {
                var synonym = new Synonym
                {
                    Word = word1,
                    SynonymWord = word2,
                    Priority = (int)(weight * 10), // 将权重转换为优先级
                    IsEnabled = true,
                    CreatedAt = DateTime.UtcNow
                };
                await _context.Synonyms.AddAsync(synonym);
            }
            else
            {
                existingSynonym.Priority = (int)(weight * 10);
                existingSynonym.UpdatedAt = DateTime.UtcNow;
                _context.Synonyms.Update(existingSynonym);
            }

            await _context.SaveChangesAsync();

            // 清除相关缓存
            await _cacheService.RemoveAsync($"synonyms:{word1}");
            await _cacheService.RemoveAsync($"synonyms:{word2}");
        }

        public async Task RemoveSynonymAsync(string word1, string word2)
        {
            var synonym = await _context.Synonyms
                .FirstOrDefaultAsync(s =>
                    (s.Word == word1 && s.SynonymWord == word2) ||
                    (s.Word == word2 && s.SynonymWord == word1));

            if (synonym != null)
            {
                synonym.IsDeleted = true;
                synonym.UpdatedAt = DateTime.UtcNow;
                _context.Synonyms.Update(synonym);
                await _context.SaveChangesAsync();

                // 清除相关缓存
                await _cacheService.RemoveAsync($"synonyms:{word1}");
                await _cacheService.RemoveAsync($"synonyms:{word2}");
            }
        }
    }
} 