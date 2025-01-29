using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZhihuClone.Core.Interfaces;

namespace ZhihuClone.Web.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirst("nameid")?.Value ?? "0");
            var notifications = await _notificationService.GetUserNotificationsAsync(userId);
            return View(notifications);
        }
    }
} 