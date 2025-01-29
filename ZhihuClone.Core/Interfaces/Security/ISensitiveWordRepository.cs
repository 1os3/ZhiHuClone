using System.Collections.Generic;
using System.Threading.Tasks;
using ZhihuClone.Core.Models.Security;

namespace ZhihuClone.Core.Interfaces.Security;

public interface ISensitiveWordRepository : IRepository<SensitiveWord>
{
    Task<List<string>> GetCategoriesAsync();
    Task<SensitiveWord?> GetByWordAsync(string word);
    Task<List<SensitiveWord>> GetAllAsync(int page = 1, int pageSize = 20);
    Task<List<SensitiveWord>> GetByCategoryAsync(string category);
    Task<List<SensitiveWord>> GetByLevelAsync(int level);
    Task<List<SensitiveWord>> GetEnabledAsync();
    Task<List<SensitiveWord>> GetPagedAsync(int page, int pageSize, string? word = null, string? category = null, int? level = null);
    Task<int> CountAsync(string? word = null, string? category = null, int? level = null);
    Task<bool> IsWordSensitiveAsync(string word);
    Task<IEnumerable<string>> FindSensitiveWordsAsync(string content);
    Task<bool> ContainsSensitiveWordsAsync(string content);
    Task<Dictionary<string, List<string>>> AnalyzeContentAsync(string content);
    Task BulkInsertAsync(IEnumerable<SensitiveWord> words);
    Task BulkUpdateAsync(IEnumerable<SensitiveWord> words);
    Task<List<SensitiveWord>> GetAllEnabledAsync();
    Task<List<SensitiveWord>> GetRegexPatternsAsync();
    Task<bool> ContainsAnyAsync(string content);
    Task<List<string>> FindMatchesAsync(string content);
    Task<(bool isClean, List<string> matches)> ValidateContentAsync(string content);
    new Task<SensitiveWord> AddAsync(SensitiveWord entity);
    Task<SensitiveWord> UpdateAsync(SensitiveWord entity);
    Task<bool> DeleteAsync(int id);
    new Task<SensitiveWord?> GetByIdAsync(int id);
    new Task<List<SensitiveWord>> GetAllAsync();
    Task<int> GetTotalCountAsync();
    Task<bool> ExistsAsync(string word);
} 