namespace ZhihuClone.Web.Models.User
{
    public class UserItemViewModel
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Nickname { get; set; } = string.Empty;
        public string? Avatar { get; set; }
        public string? Bio { get; set; }
        public string? Title { get; set; }
        public bool IsFollowing { get; set; }
        public int FollowerCount { get; set; }
        public int PostCount { get; set; }
    }
} 