using System;

namespace ZhihuClone.Web.Models.Admin
{
    public class SearchParameters
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Pattern { get; set; }
        public string? Category { get; set; }
        public string? Word { get; set; }
        public string? Level { get; set; }
        public string? Status { get; set; }
        public string? FileType { get; set; }
        public string? Signature { get; set; }
        public string? IpAddress { get; set; }
        public string? Reason { get; set; }
        public bool? IsEnabled { get; set; }
        public bool? IsBlocked { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
} 