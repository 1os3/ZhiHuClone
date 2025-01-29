using System.Collections.Generic;
using System.Security.Claims;

namespace ZhihuClone.Core.Interfaces
{
    public interface ITokenGenerator
    {
        string GenerateToken(int userId, string username, bool isAdmin, IEnumerable<string>? roles = null);
        ClaimsPrincipal ValidateToken(string token);
        string GenerateRefreshToken();
        bool IsTokenValid(string token);
    }
} 