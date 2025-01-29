using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZhihuClone.Core.Interfaces;

namespace ZhihuClone.Web.Controllers.Api
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        private int GetUserId()
        {
            var claim = User.FindFirst("nameid") ?? 
                throw new InvalidOperationException("User ID claim not found");
            return int.Parse(claim.Value);
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var userId = GetUserId();
            var notifications = await _notificationService.GetUserNotificationsAsync(userId, page, pageSize);
            var unreadCount = await _notificationService.GetUnreadCountAsync(userId);

            return Ok(new
            {
                success = true,
                notifications,
                unreadCount
            });
        }

        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = GetUserId();
            var success = await _notificationService.MarkAsReadAsync(id, userId);

            return Ok(new { success });
        }

        [HttpPost("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = GetUserId();
            var success = await _notificationService.MarkAllAsReadAsync(userId);

            return Ok(new { success });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNotification(int id)
        {
            var userId = GetUserId();
            var success = await _notificationService.DeleteNotificationAsync(id, userId);

            return Ok(new { success });
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteAllNotifications()
        {
            var userId = GetUserId();
            var success = await _notificationService.DeleteAllNotificationsAsync(userId);

            return Ok(new { success });
        }

        [HttpGet("unread")]
        public async Task<IActionResult> GetUnreadNotifications()
        {
            var userId = GetUserId();
            var notifications = await _notificationService.GetUnreadNotificationsAsync(userId);

            return Ok(new
            {
                success = true,
                notifications
            });
        }

        [HttpGet("unread/count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = GetUserId();
            var count = await _notificationService.GetUnreadCountAsync(userId);

            return Ok(new
            {
                success = true,
                count
            });
        }
    }
} 