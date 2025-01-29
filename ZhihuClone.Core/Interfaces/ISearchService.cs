using System.Collections.Generic;
using System.Threading.Tasks;
using ZhihuClone.Core.Models.Search;

namespace ZhihuClone.Core.Interfaces
{
    public interface ISearchService
    {
        Task<SearchResult> SearchAsync(SearchRequest request, int? userId);
        Task<List<string>> GetSuggestionsAsync(string keyword);
        Task<List<string>> GetHotSearchesAsync();
    }
} 