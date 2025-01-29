using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ZhihuClone.Core.Interfaces;
using ZhihuClone.Core.Models.Search;
using ZhihuClone.Web.Models.Post;

namespace ZhihuClone.Web.Controllers
{
    public class SearchController : Controller
    {
        private readonly ISearchService _searchService;
        private readonly ISearchHistoryService _searchHistoryService;
        private readonly ISearchHighlightService _highlightService;

        public SearchController(
            ISearchService searchService,
            ISearchHistoryService searchHistoryService,
            ISearchHighlightService highlightService)
        {
            _searchService = searchService;
            _searchHistoryService = searchHistoryService;
            _highlightService = highlightService;
        }

        public async Task<IActionResult> Index(string q, int page = 1)
        {
            if (string.IsNullOrWhiteSpace(q))
                return View(new Core.Models.Search.SearchResult());

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId != null)
            {
                await _searchHistoryService.RecordSearchAsync(q, int.Parse(userId));
            }

            var request = new Core.Models.Search.SearchRequest
            {
                Query = q,
                Page = page,
                PageSize = 20
            };

            var result = await _searchService.SearchAsync(request, userId != null ? int.Parse(userId) : null);

            // 高亮处理
            foreach (var post in result.Posts)
            {
                post.Title = _highlightService.HighlightTitle(post.Title, q);
                post.Content = _highlightService.HighlightText(post.Content, q);
            }

            return View(result);
        }

        [HttpGet]
        public async Task<IActionResult> Suggestions(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return Json(new string[0]);

            var suggestions = await _searchService.GetSuggestionsAsync(keyword);
            return Json(suggestions);
        }

        [HttpGet]
        public async Task<IActionResult> HotSearches()
        {
            var hotSearches = await _searchService.GetHotSearchesAsync();
            return Json(hotSearches);
        }

        [HttpGet]
        public async Task<IActionResult> History()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Json(new string[0]);

            var history = await _searchHistoryService.GetUserSearchHistoryAsync(int.Parse(userId));
            return Json(history.Select(h => h.Keyword));
        }

        [HttpPost]
        public async Task<IActionResult> ClearHistory()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return BadRequest();

            await _searchHistoryService.ClearUserSearchHistoryAsync(int.Parse(userId));
            return Ok();
        }
    }
} 