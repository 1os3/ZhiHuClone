using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using ZhihuClone.Core.Interfaces;
using ZhihuClone.Core.Interfaces.Security;
using Microsoft.Extensions.DependencyInjection;

namespace ZhihuClone.Web.Middleware
{
    public class FirewallMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<FirewallMiddleware> _logger;
        private readonly IServiceProvider _serviceProvider;

        public FirewallMiddleware(
            RequestDelegate next,
            ILogger<FirewallMiddleware> logger,
            IServiceProvider serviceProvider)
        {
            _next = next;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // 1. 检查是否需要跳过防火墙检查（例如静态文件）
            if (ShouldSkipFirewall(context))
            {
                await _next(context);
                return;
            }

            // 2. 创建作用域并获取防火墙服务
            using (var scope = _serviceProvider.CreateScope())
            {
                var firewallService = scope.ServiceProvider.GetRequiredService<IFirewallService>();

                // 3. 验证请求
                if (!await firewallService.ValidateRequestAsync(context))
                {
                    _logger.LogWarning("Request blocked by firewall: {Path}", context.Request.Path);
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsJsonAsync(new { error = "请求被防火墙拦截" });
                    return;
                }

                // 4. 继续处理请求
                await _next(context);
            }
        }

        private bool ShouldSkipFirewall(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLower();

            // 跳过静态文件
            if (path?.StartsWith("/lib/") == true ||
                path?.StartsWith("/css/") == true ||
                path?.StartsWith("/js/") == true ||
                path?.StartsWith("/images/") == true)
            {
                return true;
            }

            // 跳过健康检查
            if (path == "/health" || path == "/healthz")
            {
                return true;
            }

            // 跳过错误页面
            if (path?.StartsWith("/error/") == true)
            {
                return true;
            }

            return false;
        }
    }

    // 扩展方法，用于在Startup中注册中间件
    public static class FirewallMiddlewareExtensions
    {
        public static IApplicationBuilder UseFirewall(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<FirewallMiddleware>();
        }
    }
} 