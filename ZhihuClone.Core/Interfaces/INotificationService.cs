using System.Collections.Generic;
using System.Threading.Tasks;
using ZhihuClone.Core.Models.Notification;

namespace ZhihuClone.Core.Interfaces
{
    public interface INotificationService
    {
        Task<List<Notification>> GetUserNotificationsAsync(int userId, int page = 1, int pageSize = 20);
        Task<int> GetUnreadCountAsync(int userId);
        Task<Notification> CreateNotificationAsync(int userId, int? senderId, string type, string content, string? link);
        Task<bool> MarkAsReadAsync(int notificationId, int userId);
        Task<bool> MarkAllAsReadAsync(int userId);
        Task<bool> DeleteNotificationAsync(int notificationId, int userId);
        Task<bool> DeleteAllNotificationsAsync(int userId);
        Task<List<Notification>> GetUnreadNotificationsAsync(int userId);
        Task<bool> HasUnreadNotificationsAsync(int userId);
        Task<bool> SendSystemNotificationAsync(int userId, string content, string? link = null);
        Task SendBatchNotificationsAsync(List<int> userIds, string type, string content, string? link = null);
    }
} 