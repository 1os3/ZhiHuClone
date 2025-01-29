using System;
using System.ComponentModel.DataAnnotations;

namespace ZhihuClone.Web.Models.User
{
    public class SecuritySettingsViewModel
    {
        public bool IsEmailConfirmed { get; set; }
        public bool IsPhoneConfirmed { get; set; }
        public bool IsTwoFactorEnabled { get; set; }
        public bool IsEmailVerified { get; set; }
        public bool IsPhoneVerified { get; set; }
        public DateTime LastLoginTime { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
    }

    public class VerifyEmailViewModel
    {
        [Required(ErrorMessage = "请输入邮箱地址")]
        [EmailAddress(ErrorMessage = "请输入有效的邮箱地址")]
        public string Email { get; set; } = string.Empty;
    }

    public class VerifyPhoneViewModel
    {
        [Required(ErrorMessage = "请输入手机号码")]
        [Phone(ErrorMessage = "请输入有效的手机号码")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "请输入验证码")]
        public string VerificationCode { get; set; } = string.Empty;
    }
} 