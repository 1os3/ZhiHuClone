using System.Collections.Generic;
using System.Threading.Tasks;
using ZhihuClone.Core.Models.Security;

namespace ZhihuClone.Core.Interfaces.Security;

public interface ISpamPatternRepository : IRepository<SpamPattern>
{
    Task<List<string>> GetCategoriesAsync();
    Task<SpamPattern?> GetByPatternAsync(string pattern);
    Task<List<SpamPattern>> GetByCategoryAsync(string category);
    Task<List<SpamPattern>> GetEnabledAsync();
    Task<List<SpamPattern>> GetPagedWithFiltersAsync(int page = 1, int pageSize = 10, string? pattern = null, string? category = null, bool? isEnabled = null);
    Task<int> CountAsync(string? pattern = null, string? category = null, bool? isEnabled = null);
    Task<List<SpamPattern>> GetAllEnabledAsync();
    Task<List<SpamPattern>> GetRegexPatternsAsync();
    Task<bool> IsSpamAsync(string content);
    Task<List<string>> FindSpamPatternsAsync(string content);
    Task<Dictionary<string, List<string>>> AnalyzeContentAsync(string content);
    Task IncrementMatchCountAsync(int patternId);
    Task UpdateLastMatchTimeAsync(int patternId);
    Task<List<SpamPattern>> GetTopMatchedPatternsAsync(int count);
    Task BulkInsertAsync(IEnumerable<SpamPattern> patterns);
    Task BulkUpdateAsync(IEnumerable<SpamPattern> patterns);
    Task<SpamPattern?> GetByIdAsync(int id);
    Task<List<SpamPattern>> GetAllAsync();
    Task<SpamPattern?> GetByTypeAsync(string type);
    Task<List<SpamPattern>> GetPagedAsync(int page = 1, int pageSize = 20, string? pattern = null);
    Task<int> GetTotalCountAsync();
    Task<SpamPattern> AddAsync(SpamPattern pattern);
    Task<SpamPattern> UpdateAsync(SpamPattern pattern);
    Task DeleteAsync(int id);
    Task<bool> IsPatternExistsAsync(string pattern);
} 