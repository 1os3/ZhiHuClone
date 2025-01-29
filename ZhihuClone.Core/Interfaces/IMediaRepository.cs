using System.Collections.Generic;
using System.Threading.Tasks;
using ZhihuClone.Core.Models.Content;

namespace ZhihuClone.Core.Interfaces
{
    public interface IMediaRepository : IRepository<Media>
    {
        Task<List<Media>> GetByPostIdAsync(int postId);
        Task<List<Media>> GetByUserIdAsync(int userId);
        Task<List<Media>> GetPagedAsync(int page = 1, int pageSize = 10);
        Task<List<Media>> SearchAsync(string searchTerm, int page = 1, int pageSize = 10);
        Task<int> GetTotalCountAsync(string? searchTerm = null);
        Task<bool> IsValidTypeAsync(string contentType);
        Task<string?> GetUrlAsync(int mediaId);
        Task AssociateWithPostAsync(int mediaId, int postId);
    }
} 