using System.ComponentModel.DataAnnotations;

namespace ZhihuClone.Web.Models.User
{
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "请输入当前密码")]
        [DataType(DataType.Password)]
        [Display(Name = "当前密码")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "请输入新密码")]
        [StringLength(100, ErrorMessage = "{0}必须至少包含{2}个字符。", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "新密码")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "请确认新密码")]
        [DataType(DataType.Password)]
        [Display(Name = "确认新密码")]
        [Compare("NewPassword", ErrorMessage = "新密码和确认密码不匹配。")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
} 