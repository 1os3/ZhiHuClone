using System.Collections.Generic;
using ZhihuClone.Web.Models.User;

namespace ZhihuClone.Web.Models.User
{
    public class UserListViewModel
    {
        public string PageTitle { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Nickname { get; set; } = string.Empty;
        public List<UserItemViewModel> Users { get; set; } = new();
        public bool IsCurrentUser { get; set; }
    }
} 