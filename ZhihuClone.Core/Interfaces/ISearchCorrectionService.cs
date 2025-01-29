using System.Collections.Generic;
using System.Threading.Tasks;
using ZhihuClone.Core.Models.Search;

namespace ZhihuClone.Core.Interfaces
{
    public interface ISearchCorrectionService
    {
        Task<List<SearchSuggestion>> GetSuggestionsAsync(string searchTerm);
        Task<string> SuggestCorrectionAsync(string query);
    }
} 