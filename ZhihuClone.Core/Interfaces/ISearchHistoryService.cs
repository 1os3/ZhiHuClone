using System.Collections.Generic;
using System.Threading.Tasks;
using ZhihuClone.Core.Models.Search;

namespace ZhihuClone.Core.Interfaces
{
    public interface ISearchHistoryService
    {
        Task<List<SearchHistory>> GetUserSearchHistoriesAsync(int userId, int page = 1, int pageSize = 10);
        Task<SearchHistory> AddAsync(SearchHistory searchHistory);
        Task<bool> DeleteAsync(int id);
        Task<bool> ClearUserHistoryAsync(int userId);
        Task<List<string>> GetHotSearchesAsync(int count = 10);
        Task<List<SearchHistory>> GetRecentSearchesAsync(int userId, int count = 10);
        Task<List<string>> GetSuggestionsAsync(string keyword, int count = 10);
        Task RecordSearchAsync(string keyword, int userId);
        Task<List<string>> GetTrendingSearchesAsync(int count = 10);
        Task<bool> DeleteSearchHistoryAsync(int id);
        Task<List<SearchHistory>> GetUserSearchHistoryAsync(int userId, int page = 1, int pageSize = 10);
        Task<bool> ClearUserSearchHistoryAsync(int userId);
        Task<List<SearchHistory>> GetAllSearchHistoriesAsync(int page = 1, int pageSize = 50);
        Task<List<string>> GetRelatedSearchesAsync(string keyword, int count = 10);
    }
} 