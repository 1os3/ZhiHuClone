using System;

namespace ZhihuClone.Core.Models.Notification
{
    public class Notification
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int? SenderId { get; set; }
        public string Type { get; set; } = null!;
        public string Content { get; set; } = null!;
        public string? Link { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReadAt { get; set; }

        // 导航属性
        public virtual User User { get; set; } = null!;
        public virtual User? Sender { get; set; }
    }
}

public enum NotificationType
{
    Like,           // 点赞通知
    Comment,        // 评论通知
    Reply,          // 回复通知
    Follow,         // 关注通知
    System,         // 系统通知
    Mention         // @提及通知
} 