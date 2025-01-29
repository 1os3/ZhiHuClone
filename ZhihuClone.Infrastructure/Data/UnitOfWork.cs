using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZhihuClone.Core.Interfaces;
using ZhihuClone.Core.Interfaces.Security;
using ZhihuClone.Infrastructure.Repositories;
using ZhihuClone.Infrastructure.Repositories.Security;

namespace ZhihuClone.Infrastructure.Data;

public class UnitOfWork : IUnitOfWork, IDisposable
{
    private readonly ApplicationDbContext _context;
    private bool _disposed;

    public IUserRepository Users { get; }
    public IPostRepository Posts { get; }
    public ICommentRepository Comments { get; }
    public IMediaRepository Media { get; }
    public IBlockedIpRepository BlockedIps { get; }
    public ISensitiveWordRepository SensitiveWords { get; }
    public ISpamPatternRepository SpamPatterns { get; }
    public IFileSignatureRepository FileSignatures { get; }
    public ICommentReportRepository CommentReports { get; }

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
        Users = new UserRepository(_context);
        Posts = new PostRepository(_context);
        Comments = new CommentRepository(_context);
        Media = new MediaRepository(_context);
        BlockedIps = new BlockedIpRepository(_context);
        SensitiveWords = new SensitiveWordRepository(_context);
        SpamPatterns = new SpamPatternRepository(_context);
        FileSignatures = new FileSignatureRepository(_context);
        CommentReports = new CommentReportRepository(_context);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _context.Dispose();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
} 