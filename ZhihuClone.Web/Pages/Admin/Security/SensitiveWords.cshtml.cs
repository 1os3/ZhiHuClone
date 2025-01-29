using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ZhihuClone.Core.Interfaces.Security;
using ZhihuClone.Core.Models.Security;
using ZhihuClone.Web.Models.Admin;

namespace ZhihuClone.Web.Pages.Admin.Security
{
    public class SensitiveWordSearchParameters
    {
        public string? Word { get; set; }
        public string? Category { get; set; }
        public int? Level { get; set; }
    }

    [Authorize(Roles = "Admin")]
    public class SensitiveWordsModel : PageModel
    {
        private readonly ISensitiveWordRepository _sensitiveWordRepository;

        public SensitiveWordsModel(ISensitiveWordRepository sensitiveWordRepository)
        {
            _sensitiveWordRepository = sensitiveWordRepository;
        }

        public List<SensitiveWord> SensitiveWords { get; set; } = new();
        public List<string> Categories { get; set; } = new();
        public SensitiveWordSearchParameters SearchParams { get; set; } = new();
        public int CurrentPage { get; set; }
        public int PageSize { get; set; } = 10;
        public int TotalItems { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalItems / (double)PageSize);

        public async Task OnGetAsync(
            string? word = null,
            string? category = null,
            int? level = null,
            int page = 1)
        {
            CurrentPage = page < 1 ? 1 : page;
            SearchParams = new SensitiveWordSearchParameters
            {
                Word = word,
                Category = category,
                Level = level
            };

            // 获取所有分类
            Categories = await _sensitiveWordRepository.GetCategoriesAsync();

            // 获取总记录数
            TotalItems = await _sensitiveWordRepository.CountAsync(
                word: SearchParams.Word,
                category: SearchParams.Category,
                level: SearchParams.Level);

            // 获取分页数据
            SensitiveWords = await _sensitiveWordRepository.GetPagedAsync(
                page: CurrentPage,
                pageSize: PageSize,
                word: SearchParams.Word,
                category: SearchParams.Category,
                level: SearchParams.Level);
        }

        public string GetPageUrl(int pageNumber)
        {
            var queryParams = new List<string>();

            if (!string.IsNullOrEmpty(SearchParams.Word))
                queryParams.Add($"word={SearchParams.Word}");

            if (!string.IsNullOrEmpty(SearchParams.Category))
                queryParams.Add($"category={SearchParams.Category}");

            if (SearchParams.Level.HasValue)
                queryParams.Add($"level={SearchParams.Level}");

            queryParams.Add($"page={pageNumber}");

            return $"/Admin/Security/SensitiveWords?{string.Join("&", queryParams)}";
        }
    }
} 