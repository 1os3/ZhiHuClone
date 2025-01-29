using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ZhihuClone.Core.Interfaces.Security;
using ZhihuClone.Core.Models;
using ZhihuClone.Core.Models.Security;
using ZhihuClone.Web.Models.Admin;
using Microsoft.Extensions.Logging;

namespace ZhihuClone.Web.Pages.Admin.Security
{
    [Authorize(Roles = "Admin")]
    public class SpamPatternsModel : PageModel
    {
        private readonly ISpamPatternRepository _spamPatternRepository;
        private readonly ILogger<SpamPatternsModel> _logger;

        public List<SpamPattern> SpamPatterns { get; set; } = new();
        public List<string> Categories { get; set; } = new();
        public string? Pattern { get; set; }
        public string? Category { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalItems { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalItems / (double)PageSize);

        public SpamPatternsModel(ISpamPatternRepository spamPatternRepository, ILogger<SpamPatternsModel> logger)
        {
            _spamPatternRepository = spamPatternRepository;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync(string? pattern = null, string? category = null, int page = 1)
        {
            try
            {
                CurrentPage = page;
                Pattern = pattern;
                Category = category;

                SpamPatterns = await _spamPatternRepository.GetPagedWithFiltersAsync(
                    page: page,
                    pageSize: PageSize,
                    pattern: pattern,
                    category: category
                );

                TotalItems = await _spamPatternRepository.CountAsync(
                    pattern: pattern,
                    category: category
                );

                // Get all categories
                Categories = await _spamPatternRepository.GetCategoriesAsync();

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading spam patterns");
                return RedirectToPage("/Error");
            }
        }

        public string GetPageUrl(int pageNumber)
        {
            var queryParams = new Dictionary<string, string>();
            
            if (!string.IsNullOrEmpty(Pattern))
                queryParams["pattern"] = Pattern;
                
            if (!string.IsNullOrEmpty(Category))
                queryParams["category"] = Category;
                
            queryParams["page"] = pageNumber.ToString();

            return Url.Page("./SpamPatterns", queryParams) ?? 
                throw new InvalidOperationException("Failed to generate page URL");
        }

        public async Task<IActionResult> OnPostAddAsync([FromBody] SpamPattern pattern)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await _spamPatternRepository.AddAsync(pattern);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            await _spamPatternRepository.DeleteAsync(id);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateAsync([FromBody] SpamPattern pattern)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await _spamPatternRepository.UpdateAsync(pattern);
            return RedirectToPage();
        }
    }
} 