using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZhihuClone.Core.Interfaces;
using ZhihuClone.Core.Models.Search;

namespace ZhihuClone.Web.Pages.Admin.Search
{
    public class IndexModel : PageModel
    {
        private readonly ISearchHistoryService _searchHistoryService;
        private readonly ISearchService _searchService;

        public IndexModel(
            ISearchHistoryService searchHistoryService,
            ISearchService searchService)
        {
            _searchHistoryService = searchHistoryService;
            _searchService = searchService;

            // 初始化列表属性
            SearchHistories = new List<SearchHistoryViewModel>();
            HotSearchLabels = new List<string>();
            HotSearchData = new List<int>();
            TrendLabels = new List<string>();
            TrendData = new List<int>();
            AvgSearchDuration = "0秒";
        }

        public int TodaySearchCount { get; set; }
        public int TotalSearchCount { get; set; }
        public int UniqueUsers { get; set; }
        public string AvgSearchDuration { get; set; }
        public List<SearchHistoryViewModel> SearchHistories { get; set; }
        public List<string> HotSearchLabels { get; set; }
        public List<int> HotSearchData { get; set; }
        public List<string> TrendLabels { get; set; }
        public List<int> TrendData { get; set; }

        public async Task OnGetAsync()
        {
            var today = DateTime.UtcNow.Date;
            var histories = await _searchHistoryService.GetAllSearchHistoriesAsync();
            
            // 计算统计数据
            TodaySearchCount = histories.Count(h => h.LastSearchedAt.Date == today);
            TotalSearchCount = histories.Sum(h => h.SearchCount);
            UniqueUsers = histories.Select(h => h.UserId).Distinct().Count();
            
            // 计算平均搜索时长（示例数据）
            AvgSearchDuration = "2.5秒";

            // 获取热门搜索数据
            var hotSearches = await _searchHistoryService.GetTrendingSearchesAsync(10);
            HotSearchLabels = hotSearches;
            HotSearchData = histories
                .Where(h => hotSearches.Contains(h.Keyword))
                .GroupBy(h => h.Keyword)
                .OrderByDescending(g => g.Sum(h => h.SearchCount))
                .Select(g => g.Sum(h => h.SearchCount))
                .ToList();

            // 获取搜索趋势数据（最近7天）
            TrendLabels = Enumerable.Range(0, 7)
                .Select(i => today.AddDays(-i).ToString("MM-dd"))
                .Reverse()
                .ToList();

            TrendData = Enumerable.Range(0, 7)
                .Select(i => histories.Count(h => h.LastSearchedAt.Date == today.AddDays(-i)))
                .Reverse()
                .ToList();

            // 获取搜索历史列表
            SearchHistories = histories
                .GroupBy(h => h.Keyword)
                .Select(g => new SearchHistoryViewModel
                {
                    Id = g.First().Id,
                    Keyword = g.Key,
                    SearchCount = g.Sum(h => h.SearchCount),
                    UserCount = g.Select(h => h.UserId).Distinct().Count(),
                    FirstSearchTime = g.Min(h => h.CreatedAt),
                    LastSearchTime = g.Max(h => h.LastSearchedAt)
                })
                .OrderByDescending(h => h.LastSearchTime)
                .ToList();
        }
    }

    public class SearchHistoryViewModel
    {
        public int Id { get; set; }
        public required string Keyword { get; set; }
        public int SearchCount { get; set; }
        public int UserCount { get; set; }
        public DateTime FirstSearchTime { get; set; }
        public DateTime LastSearchTime { get; set; }
    }
} 