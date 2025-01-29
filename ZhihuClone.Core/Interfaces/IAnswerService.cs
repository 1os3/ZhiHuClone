using System.Collections.Generic;
using System.Threading.Tasks;
using ZhihuClone.Core.Models;

namespace ZhihuClone.Core.Interfaces
{
    public interface IAnswerService
    {
        Task<Answer?> GetAnswerByIdAsync(int id);
        Task<List<Answer>> GetAnswersByQuestionIdAsync(int questionId, int page = 1, int pageSize = 20);
        Task<List<Answer>> GetAnswersByUserIdAsync(int userId, int page = 1, int pageSize = 20);
        Task<Answer> CreateAnswerAsync(Answer answer);
        Task<Answer> UpdateAnswerAsync(Answer answer);
        Task<bool> DeleteAnswerAsync(int id);
        Task<bool> LikeAnswerAsync(int answerId, int userId);
        Task<bool> UnlikeAnswerAsync(int answerId, int userId);
        Task<bool> AcceptAnswerAsync(int answerId, int questionId);
    }
} 