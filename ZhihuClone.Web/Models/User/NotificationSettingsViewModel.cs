using System.ComponentModel.DataAnnotations;

namespace ZhihuClone.Web.Models.User
{
    public class NotificationSettingsViewModel
    {
        [Display(Name = "邮件通知")]
        public bool EmailNotification { get; set; }

        [Display(Name = "浏览器推送")]
        public bool PushNotification { get; set; }

        [Display(Name = "点赞通知")]
        public bool LikeNotification { get; set; }

        [Display(Name = "评论通知")]
        public bool CommentNotification { get; set; }

        [Display(Name = "关注通知")]
        public bool FollowNotification { get; set; }

        [Display(Name = "系统通知")]
        public bool SystemNotification { get; set; }
    }
} 