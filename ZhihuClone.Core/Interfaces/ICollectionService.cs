using System.Collections.Generic;
using System.Threading.Tasks;
using ZhihuClone.Core.Models;

namespace ZhihuClone.Core.Interfaces
{
    public interface ICollectionService
    {
        Task<Collection> GetByIdAsync(int id);
        Task<List<Collection>> GetUserCollectionsAsync(int userId, int page = 1, int pageSize = 10);
        Task<Collection> CreateAsync(Collection collection);
        Task<Collection> UpdateAsync(Collection collection);
        Task DeleteAsync(int id);
        Task<bool> IsFollowingAsync(int userId, int collectionId);
        Task FollowAsync(int userId, int collectionId);
        Task UnfollowAsync(int userId, int collectionId);
    }
} 