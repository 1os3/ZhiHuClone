using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using ZhihuClone.Core.Interfaces;

namespace ZhihuClone.Web.Controllers.Api
{
    [Route("api/admin/search")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminSearchController : ControllerBase
    {
        private readonly ISearchHistoryService _searchHistoryService;

        public AdminSearchController(ISearchHistoryService searchHistoryService)
        {
            _searchHistoryService = searchHistoryService;
        }

        [HttpDelete("history/{id}")]
        public async Task<IActionResult> DeleteHistory(int id)
        {
            await _searchHistoryService.DeleteSearchHistoryAsync(id);
            return Ok(new { success = true });
        }

        [HttpGet("export/hot-searches")]
        public async Task<IActionResult> ExportHotSearches()
        {
            var hotSearches = await _searchHistoryService.GetTrendingSearchesAsync(100);
            
            // 创建CSV内容
            var csv = "Rank,Keyword\n";
            for (var i = 0; i < hotSearches.Count; i++)
            {
                csv += $"{i + 1},{hotSearches[i]}\n";
            }

            // 返回CSV文件
            var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
            return File(bytes, "text/csv", "hot_searches.csv");
        }
    }
} 