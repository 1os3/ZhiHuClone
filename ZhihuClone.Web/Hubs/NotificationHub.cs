using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Identity;
using ZhihuClone.Core.Interfaces;
using ZhihuClone.Core.Models.Notification;

namespace ZhihuClone.Web.Hubs
{
    [Authorize]
    public class NotificationHub : Hub, INotificationHub
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IUserService _userService;

        public NotificationHub(IHubContext<NotificationHub> hubContext, IUserService userService)
        {
            _hubContext = hubContext;
            _userService = userService;
        }

        private int GetUserId()
        {
            var claim = Context.User?.FindFirst("nameid");
            if (claim == null || !int.TryParse(claim.Value, out int userId))
                throw new InvalidOperationException("User ID not found");
            return userId;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();
            var user = await _userService.GetByIdAsync(userId);
            if (user != null)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserId();
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId}");
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendNotificationAsync(int userId, Notification notification)
        {
            await _hubContext.Clients.Group($"User_{userId}")
                .SendAsync("ReceiveNotification", new
                {
                    notification.Id,
                    notification.Type,
                    notification.Content,
                    notification.Link,
                    notification.CreatedAt,
                    SenderName = notification.Sender?.UserName,
                    SenderAvatar = notification.Sender?.Avatar,
                    notification.IsRead
                });
        }

        public async Task MarkAsReadAsync(int userId, int notificationId)
        {
            await _hubContext.Clients.Group($"User_{userId}")
                .SendAsync("MarkAsRead", notificationId);
        }

        public async Task MarkAllAsReadAsync(int userId)
        {
            await _hubContext.Clients.Group($"User_{userId}")
                .SendAsync("MarkAllAsRead");
        }
    }
} 