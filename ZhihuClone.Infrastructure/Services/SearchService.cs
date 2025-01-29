using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZhihuClone.Core.Interfaces;
using ZhihuClone.Core.Models;
using ZhihuClone.Core.Models.Search;
using ZhihuClone.Infrastructure.Data;

namespace ZhihuClone.Infrastructure.Services
{
    public class SearchService : ISearchService
    {
        private readonly ApplicationDbContext _context;
        private readonly ISearchHistoryService _searchHistoryService;

        public SearchService(ApplicationDbContext context, ISearchHistoryService searchHistoryService)
        {
            _context = context;
            _searchHistoryService = searchHistoryService;
        }

        public async Task<SearchResult> SearchAsync(SearchRequest request, int? userId)
        {
            // 记录搜索历史
            if (userId.HasValue)
            {
                await _searchHistoryService.RecordSearchAsync(request.Query, userId.Value);
            }

            var query = _context.Posts.AsNoTracking();

            // 应用搜索条件
            if (!string.IsNullOrEmpty(request.Query))
            {
                query = query.Where(p => p.Title.Contains(request.Query) || p.Content.Contains(request.Query));
            }

            // 获取总数
            var totalCount = await query.CountAsync();

            // 应用分页
            var posts = await query
                .Include(p => p.Author)
                .Include(p => p.Topics)
                .OrderByDescending(p => p.CreatedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            // 构建搜索结果
            var result = new SearchResult
            {
                Posts = posts,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize,
                Type = SearchType.Posts.ToString()
            };

            return result;
        }

        public async Task<List<string>> GetSuggestionsAsync(string keyword)
        {
            return await _searchHistoryService.GetSuggestionsAsync(keyword);
        }

        public async Task<List<string>> GetHotSearchesAsync()
        {
            return await _searchHistoryService.GetHotSearchesAsync();
        }
    }
} 