using ZhihuClone.Infrastructure.Data;

namespace ZhihuClone.API
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<IDbConnectionFactory, SqlConnectionFactory>();
        }
    }
} 