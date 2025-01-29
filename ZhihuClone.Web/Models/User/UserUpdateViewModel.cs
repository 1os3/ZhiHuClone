using System.ComponentModel.DataAnnotations;

namespace ZhihuClone.Web.Models.User
{
    public class UserUpdateViewModel
    {
        [Required]
        [StringLength(50)]
        public string Nickname { get; set; } = string.Empty;

        [StringLength(500)]
        public string Bio { get; set; } = string.Empty;

        [StringLength(100)]
        public string Location { get; set; } = string.Empty;

        [StringLength(100)]
        public string Company { get; set; } = string.Empty;

        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [StringLength(200)]
        [Url]
        public string Website { get; set; } = string.Empty;
    }
} 