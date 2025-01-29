using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using ZhihuClone.Core.Models.Content;

namespace ZhihuClone.Core.Interfaces
{
    public interface IMediaService
    {
        Task<Media?> GetByIdAsync(int id);
        Task<List<Media>> GetAllAsync();
        Task<List<Media>> GetByPostIdAsync(int postId);
        Task<List<Media>> GetByUserIdAsync(int userId);
        Task<List<Media>> GetPagedAsync(int page = 1, int pageSize = 20);
        Task<List<Media>> SearchAsync(string searchTerm, int page = 1, int pageSize = 10);
        Task<int> GetTotalCountAsync(string? searchTerm = null);
        Task<Media> CreateAsync(Media media);
        Task UpdateAsync(Media media);
        Task DeleteAsync(int id);
        Task<bool> IsValidTypeAsync(string contentType);
        Task<string?> GetUrlAsync(int mediaId);
        Task<Media> UploadMediaAsync(IFormFile file, int userId);
        Task<List<Media>> UploadMultipleMediaAsync(IList<IFormFile> files, int userId, int? postId = null);
        Task<Media?> GetMediaByIdAsync(int id);
        Task<List<Media>> GetMediaByPostIdAsync(int postId);
        Task<bool> DeleteMediaAsync(int id);
        Task<bool> AssociateMediaWithPostAsync(int mediaId, int postId);
        Task<string> SaveFileAsync(IFormFile file);
        Task<bool> DeleteFileAsync(string filePath);
        Task<string> UploadAvatarAsync(IFormFile file);
    }
} 