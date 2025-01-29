using System.ComponentModel.DataAnnotations;

namespace ZhihuClone.Web.Models.User
{
    public class PrivacySettingsViewModel
    {
        [Display(Name = "显示邮箱")]
        public bool ShowEmail { get; set; }

        [Display(Name = "显示手机号")]
        public bool ShowPhone { get; set; }

        [Display(Name = "显示所在地")]
        public bool ShowLocation { get; set; }

        [Display(Name = "显示公司信息")]
        public bool ShowCompany { get; set; }

        [Display(Name = "允许他人关注")]
        public bool AllowFollow { get; set; }

        [Display(Name = "允许私信")]
        public bool AllowDirectMessage { get; set; }

        [Display(Name = "允许通知")]
        public bool AllowNotification { get; set; }
    }
} 