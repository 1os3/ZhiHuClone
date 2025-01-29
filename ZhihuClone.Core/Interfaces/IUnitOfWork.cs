using System;
using System.Threading.Tasks;
using ZhihuClone.Core.Interfaces.Security;

namespace ZhihuClone.Core.Interfaces;

public interface IUnitOfWork
{
    IUserRepository Users { get; }
    IPostRepository Posts { get; }
    ICommentRepository Comments { get; }
    IMediaRepository Media { get; }
    IBlockedIpRepository BlockedIps { get; }
    ISensitiveWordRepository SensitiveWords { get; }
    ISpamPatternRepository SpamPatterns { get; }
    IFileSignatureRepository FileSignatures { get; }
    ICommentReportRepository CommentReports { get; }
    Task SaveChangesAsync();
} 