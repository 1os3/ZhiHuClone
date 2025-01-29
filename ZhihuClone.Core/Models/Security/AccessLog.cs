using System;
using System.ComponentModel.DataAnnotations;

namespace ZhihuClone.Core.Models.Security
{
    public class AccessLog
    {
        public int Id { get; set; }
        
        [Required]
        public string Action { get; set; } = null!;
        
        [Required]
        public string IpAddress { get; set; } = null!;
        
        public int? UserId { get; set; }
        
        [Required]
        public string UserAgent { get; set; } = null!;
        
        [Required]
        public string RequestUrl { get; set; } = null!;
        
        [Required]
        public string RequestMethod { get; set; } = null!;
        
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public int StatusCode { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public User? User { get; set; }

        public string? Path { get; set; }
        public string? QueryString { get; set; }
        public string? RequestBody { get; set; }
        public string? Status { get; set; }
        public string? ResponseBody { get; set; }
    }
} 