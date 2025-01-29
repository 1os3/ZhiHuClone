using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ZhihuClone.Core.Interfaces;
using ZhihuClone.Core.Interfaces.Security;
using ZhihuClone.Infrastructure.Data;
using ZhihuClone.Infrastructure.Repositories;
using ZhihuClone.Infrastructure.Repositories.Security;
using ZhihuClone.Infrastructure.Security;
using ZhihuClone.Infrastructure.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Identity;
using ZhihuClone.Core.Models;

var builder = WebApplication.CreateBuilder(args);

// 添加服务到容器
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

// 配置数据库
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<DbContext>(provider => provider.GetService<ApplicationDbContext>()!);
builder.Services.AddScoped<IDbConnectionFactory, DbConnectionFactory>();

// 配置Identity
builder.Services.AddIdentity<User, IdentityRole<int>>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;

    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// 配置认证
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not found"))),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer not found"),
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience not found"),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

// 配置CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader());
});

// 配置缓存
builder.Services.AddMemoryCache();

// 注册服务
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IMediaService, MediaService>();
builder.Services.AddScoped<IFirewallService, FirewallService>();
builder.Services.AddScoped<ITokenGenerator, TokenGenerator>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();

// 注册仓储服务
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPostRepository, PostRepository>();
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<ITopicRepository, TopicRepository>();
builder.Services.AddScoped<IMediaRepository, MediaRepository>();

// 注册安全相关服务
builder.Services.AddScoped<IBlockedIpRepository, BlockedIpRepository>();
builder.Services.AddScoped<ISensitiveWordRepository, SensitiveWordRepository>();
builder.Services.AddScoped<ISpamPatternRepository, SpamPatternRepository>();
builder.Services.AddScoped<IFileSignatureRepository, FileSignatureRepository>();
builder.Services.AddScoped<IAccessLogRepository, AccessLogRepository>();

// 配置AutoMapper
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

var app = builder.Build();

// 配置HTTP请求管道
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

// 添加自定义中间件
app.Use(async (context, next) =>
{
    var firewallService = context.RequestServices.GetRequiredService<IFirewallService>();
    var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    if (await firewallService.IsIpBlockedAsync(ipAddress))
    {
        context.Response.StatusCode = 403;
        await context.Response.WriteAsync("您的IP已被封禁");
        return;
    }

    if (!await firewallService.CheckRateLimitAsync(ipAddress, context.Request.Path))
    {
        context.Response.StatusCode = 429;
        await context.Response.WriteAsync("请求过于频繁，请稍后再试");
        return;
    }

    await next();
});

app.MapControllers();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var serviceProvider = scope.ServiceProvider;
    await DbInitializer.InitializeAsync(serviceProvider);
}

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
