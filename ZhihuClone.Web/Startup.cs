using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ZhihuClone.Core.Interfaces;
using ZhihuClone.Infrastructure.Services;
using ZhihuClone.Web.Hubs;

namespace ZhihuClone.Web
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // ... existing code ...

            // 添加SignalR服务
            services.AddSignalR();

            // 注册通知服务
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<INotificationHub, NotificationHub>();

            // 注册搜索相关服务
            services.AddScoped<ISearchService, SearchService>();
            services.AddScoped<ISearchHistoryService, SearchHistoryService>();

            // ... existing code ...
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // ... existing code ...

            app.UseEndpoints(endpoints =>
            {
                // ... existing code ...
                
                // 添加SignalR Hub映射
                endpoints.MapHub<NotificationHub>("/notificationHub");
                
                // ... existing code ...
            });
        }
    }
} 