using System;
using System.ComponentModel.DataAnnotations;

namespace ZhihuClone.Core.Models.Search
{
    public class Synonym
    {
        public int Id { get; set; }

        [Required]
        public string Word { get; set; } = string.Empty;

        [Required]
        public string SynonymWord { get; set; } = string.Empty;

        public bool IsEnabled { get; set; } = true;

        public bool IsDeleted { get; set; }

        public int Priority { get; set; } = 1;

        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public int CreatedByUserId { get; set; }
        public virtual User CreatedByUser { get; set; } = null!;

        public int? UpdatedByUserId { get; set; }
        public virtual User? UpdatedByUser { get; set; }
    }
} 