using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ZhihuClone.Core.Models;
using ZhihuClone.Core.Interfaces;
using ZhihuClone.Core.Models.Security;
using ZhihuClone.Infrastructure.Security;

namespace ZhihuClone.Infrastructure.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var services = scope.ServiceProvider;
            var logger = services.GetRequiredService<ILogger<ApplicationDbContext>>();

            try
            {
                var context = services.GetRequiredService<ApplicationDbContext>();

                // 确保数据库已创建
                await context.Database.EnsureCreatedAsync();

                // 初始化管理员用户
                var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "admin@zhihuclone.com");
                if (adminUser == null)
                {
                    adminUser = new User
                    {
                        Email = "admin@zhihuclone.com",
                        UserName = "admin",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123456"),
                        IsEmailConfirmed = true,
                        IsAdmin = true,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        LastLoginAt = DateTime.UtcNow
                    };
                    await context.Users.AddAsync(adminUser);
                    await context.SaveChangesAsync();
                }

                // 初始化文件签名
                if (!await context.FileSignatures.AnyAsync())
                {
                    var fileSignatures = new List<FileSignature>
                    {
                        new FileSignature
                        {
                            Signature = "FFD8FF",
                            FileType = "image/jpeg",
                            Description = "JPEG Image",
                            IsEnabled = true,
                            CreatedAt = DateTime.UtcNow,
                            CreatedByUserId = adminUser.Id
                        },
                        new FileSignature
                        {
                            Signature = "89504E47",
                            FileType = "image/png",
                            Description = "PNG Image",
                            IsEnabled = true,
                            CreatedAt = DateTime.UtcNow,
                            CreatedByUserId = adminUser.Id
                        },
                        new FileSignature
                        {
                            Signature = "47494638",
                            FileType = "image/gif",
                            Description = "GIF Image",
                            IsEnabled = true,
                            CreatedAt = DateTime.UtcNow,
                            CreatedByUserId = adminUser.Id
                        },
                        new FileSignature
                        {
                            Signature = "25504446",
                            FileType = "application/pdf",
                            Description = "PDF Document",
                            IsEnabled = true,
                            CreatedAt = DateTime.UtcNow,
                            CreatedByUserId = adminUser.Id
                        }
                    };

                    await context.FileSignatures.AddRangeAsync(fileSignatures);
                }

                // 初始化敏感词
                if (!await context.SensitiveWords.AnyAsync())
                {
                    var sensitiveWords = new List<SensitiveWord>
                    {
                        new SensitiveWord
                        {
                            Word = "暴力",
                            Category = "Violence",
                            Level = 2,
                            IsEnabled = true,
                            CreatedAt = DateTime.UtcNow,
                            CreatedByUserId = adminUser.Id
                        },
                        new SensitiveWord
                        {
                            Word = "色情",
                            Category = "Adult",
                            Level = 2,
                            IsEnabled = true,
                            CreatedAt = DateTime.UtcNow,
                            CreatedByUserId = adminUser.Id
                        },
                        new SensitiveWord
                        {
                            Word = "赌博",
                            Category = "Gambling",
                            Level = 2,
                            IsEnabled = true,
                            CreatedAt = DateTime.UtcNow,
                            CreatedByUserId = adminUser.Id
                        }
                    };

                    await context.SensitiveWords.AddRangeAsync(sensitiveWords);
                }

                // 初始化垃圾信息模式
                if (!await context.SpamPatterns.AnyAsync())
                {
                    var spamPatterns = new List<SpamPattern>
                    {
                        new SpamPattern
                        {
                            Pattern = @"\b(viagra|cialis)\b",
                            Category = "Drug",
                            IsRegex = true,
                            IsEnabled = true,
                            CreatedAt = DateTime.UtcNow,
                            CreatedByUserId = adminUser.Id
                        },
                        new SpamPattern
                        {
                            Pattern = @"\b(casino|gambling|bet)\b",
                            Category = "Gambling",
                            IsRegex = true,
                            IsEnabled = true,
                            CreatedAt = DateTime.UtcNow,
                            CreatedByUserId = adminUser.Id
                        },
                        new SpamPattern
                        {
                            Pattern = @"(\$\d+.*?hour|\d+\$.*?day)",
                            Category = "Money",
                            IsRegex = true,
                            IsEnabled = true,
                            CreatedAt = DateTime.UtcNow,
                            CreatedByUserId = adminUser.Id
                        }
                    };

                    await context.SpamPatterns.AddRangeAsync(spamPatterns);
                }

                await context.SaveChangesAsync();
                logger.LogInformation("数据库初始化完成");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "初始化数据库时发生错误");
                throw;
            }
        }
    }
} 