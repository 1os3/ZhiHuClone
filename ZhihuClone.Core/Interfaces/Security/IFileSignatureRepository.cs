using System.Collections.Generic;
using System.Threading.Tasks;
using ZhihuClone.Core.Models.Security;

namespace ZhihuClone.Core.Interfaces.Security;

public interface IFileSignatureRepository : IRepository<FileSignature>
{
    Task<List<string>> GetFileTypesAsync();
    Task<FileSignature?> GetBySignatureAsync(string signature);
    Task<List<FileSignature>> GetByFileTypeAsync(string fileType);
    Task<List<FileSignature>> GetWhitelistedAsync();
    Task<List<FileSignature>> GetPagedAsync(int page = 1, int pageSize = 10, string? signature = null, string? fileType = null, bool? isWhitelisted = null);
    Task<int> CountAsync(string? signature = null, string? fileType = null, bool? isWhitelisted = null);
    Task BulkInsertAsync(IEnumerable<FileSignature> signatures);
    Task BulkUpdateAsync(IEnumerable<FileSignature> signatures);
    Task IncrementUseCountAsync(string signature);
    Task UpdateLastUsedTimeAsync(string signature);
    Task<List<FileSignature>> GetMostUsedSignaturesAsync(int count);
    new Task<List<FileSignature>> GetAllAsync(int page = 1, int pageSize = 20);
    new Task<FileSignature> AddAsync(FileSignature fileSignature);
    Task<FileSignature> UpdateAsync(FileSignature fileSignature);
    Task<bool> DeleteAsync(int id);
    new Task<FileSignature?> GetByIdAsync(int id);
    new Task<List<FileSignature>> GetAllAsync();
    Task<FileSignature?> GetByHashAsync(string hash);
    Task<List<FileSignature>> GetByUserIdAsync(int userId);
    Task<int> GetTotalCountAsync();
    Task<bool> ExistsAsync(string hash);
} 