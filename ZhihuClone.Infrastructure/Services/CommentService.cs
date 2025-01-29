using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZhihuClone.Core.Models;
using ZhihuClone.Core.Interfaces;
using System.Data;
using Dapper;
using ZhihuClone.Infrastructure.Data;
using ZhihuClone.Core.Models.Content;

namespace ZhihuClone.Infrastructure.Services
{
    public class CommentService : ICommentService
    {
        private readonly ICommentRepository _commentRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public CommentService(ICommentRepository commentRepository, IUnitOfWork unitOfWork, IDbConnectionFactory dbConnectionFactory)
        {
            _commentRepository = commentRepository;
            _unitOfWork = unitOfWork;
            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task<Comment?> GetByIdAsync(int id)
        {
            return await _commentRepository.GetByIdAsync(id);
        }

        public async Task<List<Comment>> GetAllAsync()
        {
            return await _commentRepository.GetAllAsync();
        }

        public async Task<List<Comment>> GetByPostIdAsync(int postId)
        {
            return await _commentRepository.GetByPostIdAsync(postId);
        }

        public async Task<List<Comment>> GetByUserIdAsync(int userId)
        {
            return await _commentRepository.GetByUserIdAsync(userId);
        }

        public async Task<List<Comment>> GetRepliesAsync(int commentId)
        {
            return await _commentRepository.GetRepliesAsync(commentId);
        }

        public async Task<List<Comment>> GetPagedAsync(int page = 1, int pageSize = 20)
        {
            return await _commentRepository.GetPagedAsync(page, pageSize);
        }

        public async Task<List<Comment>> SearchAsync(string searchTerm, int page = 1, int pageSize = 10)
        {
            return await _commentRepository.SearchAsync(searchTerm, page, pageSize);
        }

        public async Task<int> GetTotalCountAsync(string? searchTerm = null)
        {
            return await _commentRepository.GetTotalCountAsync(searchTerm);
        }

        public async Task<Comment> CreateAsync(Comment comment)
        {
            var result = await _commentRepository.AddAsync(comment);
            await _unitOfWork.SaveChangesAsync();
            return result;
        }

        public async Task UpdateAsync(Comment comment)
        {
            _commentRepository.Update(comment);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            await _commentRepository.RemoveAsync(id);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<bool> LikeAsync(int commentId, int userId)
        {
            var comment = await _commentRepository.GetByIdAsync(commentId);
            if (comment == null)
            {
                return false;
            }

            comment.LikeCount++;
            _commentRepository.Update(comment);
            await _unitOfWork.SaveChangesAsync();
            
            var query = "INSERT INTO CommentLikes (CommentId, UserId) VALUES (@CommentId, @UserId)";
            using (var connection = _dbConnectionFactory.CreateConnection())
            {
                var result = await connection.ExecuteAsync(query, new { CommentId = commentId, UserId = userId });
                return result > 0;
            }
        }

        public async Task<bool> UnlikeAsync(int commentId, int userId)
        {
            var comment = await _commentRepository.GetByIdAsync(commentId);
            if (comment == null)
            {
                return false;
            }

            if (comment.LikeCount > 0)
            {
                comment.LikeCount--;
                _commentRepository.Update(comment);
                await _unitOfWork.SaveChangesAsync();
                
                var query = "DELETE FROM CommentLikes WHERE CommentId = @CommentId AND UserId = @UserId";
                using (var connection = _dbConnectionFactory.CreateConnection())
                {
                    var result = await connection.ExecuteAsync(query, new { CommentId = commentId, UserId = userId });
                    return result > 0;
                }
            }
            return false;
        }

        public async Task<bool> IsLikedByUserAsync(int commentId, int userId)
        {
            var query = @"
                SELECT COUNT(1) 
                FROM CommentLikes 
                WHERE CommentId = @CommentId AND UserId = @UserId";

            using (var connection = _dbConnectionFactory.CreateConnection())
            {
                var count = await connection.ExecuteScalarAsync<int>(query, new { CommentId = commentId, UserId = userId });
                return count > 0;
            }
        }

        public async Task<CommentReport> ReportAsync(int commentId, int reporterId, string reason, string description)
        {
            var report = new CommentReport
            {
                CommentId = commentId,
                ReporterId = reporterId,
                Reason = reason,
                Description = description,
                CreatedAt = DateTime.UtcNow,
                IsProcessed = false
            };

            await _unitOfWork.CommentReports.AddAsync(report);
            await _unitOfWork.SaveChangesAsync();
            return report;
        }

        public async Task<List<CommentReport>> GetReportsAsync(int page = 1, int pageSize = 20)
        {
            var reports = await _unitOfWork.CommentReports.GetAll();
            return reports
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        public async Task<List<CommentReport>> GetUnprocessedReportsAsync(int page = 1, int pageSize = 20)
        {
            var reports = await _unitOfWork.CommentReports.GetAll();
            return reports
                .Where(r => !r.IsProcessed)
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        public async Task ProcessReportAsync(int reportId, int processedByUserId, string processResult)
        {
            var report = await _unitOfWork.CommentReports.GetByIdAsync(reportId);
            if (report == null)
            {
                throw new KeyNotFoundException($"Report with ID {reportId} not found.");
            }

            report.ProcessedByUserId = processedByUserId;
            report.ProcessedAt = DateTime.UtcNow;
            report.ProcessResult = processResult;
            report.IsProcessed = true;

            await _unitOfWork.CommentReports.UpdateAsync(report);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<int> GetLikeCountAsync(int commentId)
        {
            return await _commentRepository.GetLikeCountAsync(commentId);
        }

        public async Task<int> GetReplyCountAsync(int commentId)
        {
            return await _commentRepository.GetReplyCountAsync(commentId);
        }

        public async Task<Comment?> GetCommentByIdAsync(int id)
        {
            return await _unitOfWork.Comments.GetByIdAsync(id);
        }

        public async Task<List<Comment>> GetCommentsByPostIdAsync(int postId, int page = 1, int pageSize = 20)
        {
            var skip = (page - 1) * pageSize;
            var query = @"
                SELECT c.*, u.Username as AuthorName, u.Avatar as AuthorAvatar 
                FROM Comments c
                JOIN Users u ON c.UserId = u.Id
                WHERE c.PostId = @PostId AND c.ParentId IS NULL
                ORDER BY c.CreatedAt DESC
                OFFSET @Skip ROWS
                FETCH NEXT @PageSize ROWS ONLY";

            using (var connection = _dbConnectionFactory.CreateConnection())
            {
                var comments = await connection.QueryAsync<Comment>(query, new { PostId = postId, Skip = skip, PageSize = pageSize });
                return comments.ToList();
            }
        }

        public async Task<List<Comment>> GetRepliesByCommentIdAsync(int commentId, int page = 1, int pageSize = 20)
        {
            var skip = (page - 1) * pageSize;
            var query = @"
                SELECT c.*, u.Username as AuthorName, u.Avatar as AuthorAvatar 
                FROM Comments c
                JOIN Users u ON c.UserId = u.Id
                WHERE c.ParentId = @CommentId
                ORDER BY c.CreatedAt ASC
                OFFSET @Skip ROWS
                FETCH NEXT @PageSize ROWS ONLY";

            using (var connection = _dbConnectionFactory.CreateConnection())
            {
                var replies = await connection.QueryAsync<Comment>(query, new { CommentId = commentId, Skip = skip, PageSize = pageSize });
                return replies.ToList();
            }
        }

        public async Task<Comment> CreateCommentAsync(Comment comment)
        {
            comment.CreatedAt = DateTime.UtcNow;
            await _unitOfWork.Comments.AddAsync(comment);
            await _unitOfWork.SaveChangesAsync();
            return comment;
        }

        public async Task<Comment> UpdateCommentAsync(Comment comment)
        {
            comment.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Comments.UpdateAsync(comment);
            await _unitOfWork.SaveChangesAsync();
            return comment;
        }

        public async Task<bool> DeleteCommentAsync(int id)
        {
            var comment = await GetCommentByIdAsync(id);
            if (comment == null) return false;

            await _unitOfWork.Comments.DeleteAsync(comment.Id);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> LikeCommentAsync(int commentId, int userId)
        {
            var query = "INSERT INTO CommentLikes (CommentId, UserId) VALUES (@CommentId, @UserId)";
            using (var connection = _dbConnectionFactory.CreateConnection())
            {
                var result = await connection.ExecuteAsync(query, new { CommentId = commentId, UserId = userId });
                return result > 0;
            }
        }

        public async Task<bool> UnlikeCommentAsync(int commentId, int userId)
        {
            var query = "DELETE FROM CommentLikes WHERE CommentId = @CommentId AND UserId = @UserId";
            using (var connection = _dbConnectionFactory.CreateConnection())
            {
                var result = await connection.ExecuteAsync(query, new { CommentId = commentId, UserId = userId });
                return result > 0;
            }
        }

        public async Task<bool> IsCommentLikedByUserAsync(int commentId, string? username)
        {
            if (string.IsNullOrEmpty(username)) return false;

            var query = @"
                SELECT COUNT(1) 
                FROM CommentLikes cl
                JOIN Users u ON cl.UserId = u.Id
                WHERE cl.CommentId = @CommentId AND u.Username = @Username";

            using (var connection = _dbConnectionFactory.CreateConnection())
            {
                var count = await connection.ExecuteScalarAsync<int>(query, new { CommentId = commentId, Username = username });
                return count > 0;
            }
        }

        public async Task<int> GetCommentCountAsync(int postId)
        {
            var query = "SELECT COUNT(1) FROM Comments WHERE PostId = @PostId";
            using (var connection = _dbConnectionFactory.CreateConnection())
            {
                return await connection.ExecuteScalarAsync<int>(query, new { PostId = postId });
            }
        }

        public async Task<bool> ReportCommentAsync(int commentId, int userId, string reason, string description)
        {
            var query = @"
                INSERT INTO CommentReports (CommentId, UserId, Reason, Description, CreatedAt)
                VALUES (@CommentId, @UserId, @Reason, @Description, @CreatedAt)";

            using (var connection = _dbConnectionFactory.CreateConnection())
            {
                var result = await connection.ExecuteAsync(query, new
                {
                    CommentId = commentId,
                    UserId = userId,
                    Reason = reason,
                    Description = description,
                    CreatedAt = DateTime.UtcNow
                });
                return result > 0;
            }
        }

        public async Task<IEnumerable<Comment>> GetCommentsByUserIdAsync(int userId)
        {
            return await _unitOfWork.Comments.GetByUserIdAsync(userId);
        }

        public async Task<int> GetLikesCountAsync(int commentId)
        {
            return await _unitOfWork.Comments.GetLikeCountAsync(commentId);
        }

        public async Task<(List<Comment> Comments, int TotalCount)> GetPagedCommentsByPostAsync(int postId, int page, int pageSize)
        {
            var comments = await _unitOfWork.Comments.GetPagedByPostAsync(postId, page, pageSize);
            var totalCount = await _unitOfWork.Comments.CountByPostAsync(postId);
            return (comments, totalCount);
        }

        public async Task<List<Comment>> GetTopCommentsAsync(int postId, int count = 5)
        {
            return await _unitOfWork.Comments.GetTopCommentsByPostAsync(postId, count);
        }

        public async Task<List<Comment>> GetRecentCommentsAsync(int postId, int count = 5)
        {
            return await _unitOfWork.Comments.GetRecentCommentsByPostAsync(postId, count);
        }
    }
} 