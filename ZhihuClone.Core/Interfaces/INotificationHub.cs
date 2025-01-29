using System.Threading.Tasks;
using ZhihuClone.Core.Models.Notification;

namespace ZhihuClone.Core.Interfaces
{
    public interface INotificationHub
    {
        Task SendNotificationAsync(int userId, Notification notification);
        Task MarkAsReadAsync(int userId, int notificationId);
        Task MarkAllAsReadAsync(int userId);
    }
} 