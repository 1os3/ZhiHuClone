using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZhihuClone.Core.Interfaces;
using ZhihuClone.Core.Models.Notification;
using ZhihuClone.Infrastructure.Data;

namespace ZhihuClone.Infrastructure.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationHub _notificationHub;

        public NotificationService(ApplicationDbContext context, INotificationHub notificationHub)
        {
            _context = context;
            _notificationHub = notificationHub;
        }

        public async Task<List<Notification>> GetUserNotificationsAsync(int userId, int page = 1, int pageSize = 20)
        {
            return await _context.Notifications
                .Include(n => n.Sender)
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .CountAsync();
        }

        public async Task<Notification> CreateNotificationAsync(int userId, int? senderId, string type, string content, string? link)
        {
            var notification = new Notification
            {
                UserId = userId,
                SenderId = senderId,
                Type = type,
                Content = content,
                Link = link,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // 加载发送者信息
            if (senderId.HasValue)
            {
                await _context.Entry(notification)
                    .Reference(n => n.Sender)
                    .LoadAsync();
            }

            // 通过SignalR发送实时通知
            await _notificationHub.SendNotificationAsync(userId, notification);

            return notification;
        }

        public async Task<bool> MarkAsReadAsync(int notificationId, int userId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (notification == null)
                return false;

            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // 通知客户端更新状态
            await _notificationHub.MarkAsReadAsync(userId, notificationId);

            return true;
        }

        public async Task<bool> MarkAllAsReadAsync(int userId)
        {
            var unreadNotifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            if (!unreadNotifications.Any())
                return false;

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // 通知客户端更新状态
            await _notificationHub.MarkAllAsReadAsync(userId);

            return true;
        }

        public async Task<bool> DeleteNotificationAsync(int notificationId, int userId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (notification == null)
                return false;

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteAllNotificationsAsync(int userId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .ToListAsync();

            if (!notifications.Any())
                return false;

            _context.Notifications.RemoveRange(notifications);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<List<Notification>> GetUnreadNotificationsAsync(int userId)
        {
            return await _context.Notifications
                .Include(n => n.Sender)
                .Where(n => n.UserId == userId && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> HasUnreadNotificationsAsync(int userId)
        {
            return await _context.Notifications
                .AnyAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task<bool> SendSystemNotificationAsync(int userId, string content, string? link = null)
        {
            await CreateNotificationAsync(userId, null, NotificationType.System.ToString(), content, link);
            return true;
        }

        public async Task SendBatchNotificationsAsync(List<int> userIds, string type, string content, string? link = null)
        {
            var notifications = userIds.Select(userId => new Notification
            {
                UserId = userId,
                Type = type,
                Content = content,
                Link = link,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            await _context.Notifications.AddRangeAsync(notifications);
            await _context.SaveChangesAsync();

            // 通过SignalR发送实时通知
            foreach (var notification in notifications)
            {
                await _notificationHub.SendNotificationAsync(notification.UserId, notification);
            }
        }
    }
} 