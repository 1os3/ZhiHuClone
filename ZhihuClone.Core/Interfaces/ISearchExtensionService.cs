using System.Threading.Tasks;

namespace ZhihuClone.Core.Interfaces
{
    public interface ISearchExtensionService
    {
        Task<string> GetSearchTextAsync(string text);
        Task<string> ExpandKeywordsAsync(string keyword);
        Task<string[]> GetSynonymsAsync(string word);
        Task AddSynonymAsync(string word1, string word2, float weight = 1.0f);
        Task RemoveSynonymAsync(string word1, string word2);
    }
} 