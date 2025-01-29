using System.ComponentModel.DataAnnotations;

namespace ZhihuClone.Web.Models.User
{
    public class DeleteAccountViewModel
    {
        [Required(ErrorMessage = "请输入密码")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "请输入确认文字")]
        [RegularExpression("DELETE", ErrorMessage = "请输入DELETE来确认注销账号")]
        public string ConfirmText { get; set; } = string.Empty;
    }
} 