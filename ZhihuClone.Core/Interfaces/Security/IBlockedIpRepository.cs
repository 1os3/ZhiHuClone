using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZhihuClone.Core.Models.Security;

namespace ZhihuClone.Core.Interfaces.Security;

public interface IBlockedIpRepository
{
    Task<BlockedIp?> GetByIdAsync(int id);
    Task<List<BlockedIp>> GetAllAsync(string? ipAddress = null, string? status = null);
    Task<List<BlockedIp>> GetPagedAsync(int page = 1, int pageSize = 20, string? ipAddress = null, string? status = null, string? reason = null);
    Task<BlockedIp> AddAsync(BlockedIp blockedIp);
    Task<BlockedIp> UpdateAsync(BlockedIp blockedIp);
    Task DeleteAsync(int id);
    Task<bool> IsBlockedAsync(string ipAddress);
    Task<int> GetActiveBlockCountAsync();
    Task<List<BlockedIp>> GetActiveBlocksAsync(int page = 1, int pageSize = 20);
    Task<int> CountAsync(string? ipAddress = null, string? status = null, string? reason = null);
    Task<BlockedIp?> GetByIpAsync(string ipAddress);
    Task<List<BlockedIp>> GetHistoryAsync(string ipAddress, int page = 1, int pageSize = 20);
} 