using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZhihuClone.Core.Interfaces;
using ZhihuClone.Core.Models.Content;
using ZhihuClone.Infrastructure.Data;

namespace ZhihuClone.Infrastructure.Repositories
{
    public class CommentReportRepository : BaseRepository<CommentReport>, ICommentReportRepository
    {
        public CommentReportRepository(ApplicationDbContext context) : base(context)
        {
        }

        public override async Task<CommentReport?> GetByIdAsync(int id)
        {
            return await _context.Set<CommentReport>().FindAsync(id);
        }

        public async Task<List<CommentReport>> GetAll()
        {
            return await _context.Set<CommentReport>().ToListAsync();
        }

        public override async Task<CommentReport> AddAsync(CommentReport report)
        {
            await _context.Set<CommentReport>().AddAsync(report);
            return report;
        }

        public override async Task<CommentReport> UpdateAsync(CommentReport report)
        {
            _context.Entry(report).State = EntityState.Modified;
            return report;
        }

        public async Task DeleteAsync(int id)
        {
            var report = await GetByIdAsync(id);
            if (report != null)
            {
                _context.Set<CommentReport>().Remove(report);
            }
        }
    }
} 