using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZhihuClone.Core.Interfaces;
using ZhihuClone.Core.Models.Search;

namespace ZhihuClone.Web.Pages.Admin.Search
{
    public class DetailsModel : PageModel
    {
        private readonly ISearchHistoryService _searchHistoryService;
        private readonly IUserService _userService;

        public DetailsModel(ISearchHistoryService searchHistoryService, IUserService userService)
        {
            _searchHistoryService = searchHistoryService;
            _userService = userService;
            RelatedSearches = new List<RelatedSearchViewModel>();
            SearchUsers = new List<SearchUserViewModel>();
            TrendLabels = new List<string>();
            TrendData = new List<int>();
        }

        public SearchHistoryDetailViewModel? SearchHistory { get; set; }
        public List<RelatedSearchViewModel> RelatedSearches { get; set; }
        public List<SearchUserViewModel> SearchUsers { get; set; }
        public List<string> TrendLabels { get; set; }
        public List<int> TrendData { get; set; }
        public UserDetailsViewModel? UserDetails { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var histories = await _searchHistoryService.GetAllSearchHistoriesAsync();
            var targetHistory = histories.FirstOrDefault(h => h.Id == id);

            if (targetHistory == null)
                return NotFound();

            // 获取基本信息
            SearchHistory = new SearchHistoryDetailViewModel
            {
                Id = targetHistory.Id,
                Keyword = targetHistory.Keyword,
                SearchCount = histories.Where(h => h.Keyword == targetHistory.Keyword).Sum(h => h.SearchCount),
                UserCount = histories.Where(h => h.Keyword == targetHistory.Keyword).Select(h => h.UserId).Distinct().Count()
            };

            // 获取相关搜索
            var relatedKeywords = await _searchHistoryService.GetRelatedSearchesAsync(targetHistory.Keyword, 10);
            RelatedSearches = histories
                .Where(h => relatedKeywords.Contains(h.Keyword))
                .GroupBy(h => h.Keyword)
                .Select(g => new RelatedSearchViewModel
                {
                    Keyword = g.Key,
                    SearchCount = g.Sum(h => h.SearchCount)
                })
                .OrderByDescending(r => r.SearchCount)
                .ToList();

            // 获取搜索用户
            SearchUsers = histories
                .Where(h => h.Keyword == targetHistory.Keyword)
                .GroupBy(h => h.UserId)
                .Select(g => new SearchUserViewModel
                {
                    UserName = g.First().User?.UserName ?? "未知用户",
                    SearchCount = g.Sum(h => h.SearchCount),
                    FirstSearchTime = g.Min(h => h.CreatedAt),
                    LastSearchTime = g.Max(h => h.LastSearchedAt)
                })
                .OrderByDescending(u => u.SearchCount)
                .ToList();

            // 获取搜索趋势数据（最近7天）
            var today = DateTime.UtcNow.Date;
            TrendLabels = Enumerable.Range(0, 7)
                .Select(i => today.AddDays(-i).ToString("MM-dd"))
                .Reverse()
                .ToList();

            TrendData = Enumerable.Range(0, 7)
                .Select(i => histories
                    .Count(h => h.Keyword == targetHistory.Keyword && h.LastSearchedAt.Date == today.AddDays(-i)))
                .Reverse()
                .ToList();

            var userId = targetHistory.UserId;
            var user = await _userService.GetByIdAsync(userId);
            if (user != null)
            {
                UserDetails = new UserDetailsViewModel
                {
                    Id = user.Id,
                    UserName = user.UserName ?? string.Empty,
                    // ... other properties ...
                };
            }

            return Page();
        }
    }

    public class SearchHistoryDetailViewModel
    {
        public int Id { get; set; }
        public required string Keyword { get; set; }
        public int SearchCount { get; set; }
        public int UserCount { get; set; }
    }

    public class RelatedSearchViewModel
    {
        public required string Keyword { get; set; }
        public int SearchCount { get; set; }
    }

    public class SearchUserViewModel
    {
        public required string UserName { get; set; }
        public int SearchCount { get; set; }
        public DateTime FirstSearchTime { get; set; }
        public DateTime LastSearchTime { get; set; }
    }

    public class UserDetailsViewModel
    {
        public int Id { get; set; }
        public required string UserName { get; set; }
        // ... other properties ...
    }
} 