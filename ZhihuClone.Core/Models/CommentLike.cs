using System;
using ZhihuClone.Core.Models.Content;

namespace ZhihuClone.Core.Models;

public class CommentLike
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int CommentId { get; set; }
    public DateTime CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
    public virtual Comment Comment { get; set; } = null!;

    // Shadow properties for EF Core
    public int CommentId1 { get; set; }
    public int UserId1 { get; set; }
} 