using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace ZhihuClone.Core.Interfaces.Security
{
    public interface IFirewallService
    {
        /// <summary>
        /// 验证请求是否合法
        /// </summary>
        Task<bool> ValidateRequestAsync(HttpContext context);

        /// <summary>
        /// 检查IP是否被封禁
        /// </summary>
        Task<bool> IsIpBlockedAsync(string ipAddress);

        /// <summary>
        /// 封禁IP
        /// </summary>
        Task BlockIpAsync(string ipAddress, string reason);

        /// <summary>
        /// 解除IP封禁
        /// </summary>
        Task UnblockIpAsync(string ipAddress);

        /// <summary>
        /// 检查请求频率限制
        /// </summary>
        Task<bool> CheckRateLimitAsync(string ipAddress, string action);

        /// <summary>
        /// 验证文件内容类型
        /// </summary>
        Task<bool> ValidateFileContentType(IFormFile file, string declaredContentType);

        /// <summary>
        /// 检查文件是否包含XSS攻击
        /// </summary>
        Task<bool> ContainsXss(IFormFile file);

        /// <summary>
        /// 检查文件是否包含SQL注入
        /// </summary>
        Task<bool> ContainsSqlInjection(IFormFile file);

        /// <summary>
        /// 获取IP信誉分数
        /// </summary>
        Task<int> GetIpReputationScore(string ipAddress);

        /// <summary>
        /// 检查是否是机器人请求
        /// </summary>
        Task<bool> IsBotRequest(string userAgent, string ipAddress);

        /// <summary>
        /// 验证输入内容
        /// </summary>
        Task<bool> ValidateInputAsync(string input, string inputType);

        Task LogAccessAsync(string ipAddress, string action, bool isSuccess);
        Task<bool> ContainsSensitiveContentAsync(string content);
        Task<bool> IsSpamContentAsync(string content);
    }
} 