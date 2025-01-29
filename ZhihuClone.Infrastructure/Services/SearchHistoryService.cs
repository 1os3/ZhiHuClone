using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZhihuClone.Core.Interfaces;
using ZhihuClone.Core.Models.Search;
using ZhihuClone.Infrastructure.Data;

namespace ZhihuClone.Infrastructure.Services
{
    public class SearchHistoryService : ISearchHistoryService
    {
        private readonly ApplicationDbContext _context;

        public SearchHistoryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task RecordSearchAsync(string keyword, int userId)
        {
            var searchHistory = new SearchHistory
            {
                UserId = userId,
                Keyword = keyword,
                CreatedAt = DateTime.UtcNow
            };

            await AddAsync(searchHistory);
        }

        public async Task<List<SearchHistory>> GetUserSearchHistoriesAsync(int userId, int page = 1, int pageSize = 10)
        {
            return await _context.SearchHistories
                .Where(sh => sh.UserId == userId)
                .OrderByDescending(sh => sh.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<SearchHistory> AddAsync(SearchHistory searchHistory)
        {
            searchHistory.CreatedAt = DateTime.UtcNow;
            _context.SearchHistories.Add(searchHistory);
            await _context.SaveChangesAsync();
            return searchHistory;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var searchHistory = await _context.SearchHistories.FindAsync(id);
            if (searchHistory == null)
                return false;

            _context.SearchHistories.Remove(searchHistory);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ClearUserHistoryAsync(int userId)
        {
            var histories = await _context.SearchHistories
                .Where(sh => sh.UserId == userId)
                .ToListAsync();

            _context.SearchHistories.RemoveRange(histories);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<string>> GetHotSearchesAsync(int count = 10)
        {
            return await _context.SearchHistories
                .GroupBy(sh => sh.Keyword)
                .OrderByDescending(g => g.Count())
                .Take(count)
                .Select(g => g.Key)
                .ToListAsync();
        }

        public async Task<List<SearchHistory>> GetRecentSearchesAsync(int userId, int count = 10)
        {
            return await _context.SearchHistories
                .Where(sh => sh.UserId == userId)
                .OrderByDescending(sh => sh.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<string>> GetSuggestionsAsync(string keyword, int count = 10)
        {
            return await _context.SearchHistories
                .Where(sh => sh.Keyword.Contains(keyword))
                .GroupBy(sh => sh.Keyword)
                .OrderByDescending(g => g.Count())
                .Take(count)
                .Select(g => g.Key)
                .ToListAsync();
        }

        public async Task<List<string>> GetTrendingSearchesAsync(int count = 10)
        {
            var startTime = DateTime.UtcNow.AddDays(-7);
            return await _context.SearchHistories
                .Where(sh => sh.CreatedAt >= startTime)
                .GroupBy(sh => sh.Keyword)
                .OrderByDescending(g => g.Count())
                .Take(count)
                .Select(g => g.Key)
                .ToListAsync();
        }

        public async Task<bool> DeleteSearchHistoryAsync(int id)
        {
            var searchHistory = await _context.SearchHistories.FindAsync(id);
            if (searchHistory == null)
                return false;

            _context.SearchHistories.Remove(searchHistory);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<SearchHistory>> GetUserSearchHistoryAsync(int userId, int page = 1, int pageSize = 10)
        {
            return await _context.SearchHistories
                .Where(sh => sh.UserId == userId)
                .OrderByDescending(sh => sh.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<bool> ClearUserSearchHistoryAsync(int userId)
        {
            var histories = await _context.SearchHistories
                .Where(sh => sh.UserId == userId)
                .ToListAsync();

            _context.SearchHistories.RemoveRange(histories);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<SearchHistory>> GetAllSearchHistoriesAsync(int page = 1, int pageSize = 50)
        {
            return await _context.SearchHistories
                .OrderByDescending(sh => sh.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<string>> GetRelatedSearchesAsync(string keyword, int count = 10)
        {
            return await _context.SearchHistories
                .Where(sh => sh.Keyword.Contains(keyword) || keyword.Contains(sh.Keyword))
                .GroupBy(sh => sh.Keyword)
                .OrderByDescending(g => g.Count())
                .Take(count)
                .Select(g => g.Key)
                .ToListAsync();
        }
    }
} 