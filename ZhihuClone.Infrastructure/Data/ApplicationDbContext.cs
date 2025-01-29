using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ZhihuClone.Core.Models;
using ZhihuClone.Core.Models.Security;
using ZhihuClone.Core.Models.Notification;
using ZhihuClone.Core.Models.Search;
using ZhihuClone.Core.Models.Content;

namespace ZhihuClone.Infrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext<User, IdentityRole<int>, int>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Post> Posts { get; set; } = null!;
        public DbSet<Comment> Comments { get; set; } = null!;
        public DbSet<Topic> Topics { get; set; } = null!;
        public DbSet<Media> Media { get; set; } = null!;
        public DbSet<Collection> Collections { get; set; } = null!;
        public DbSet<Report> Reports { get; set; } = null!;
        public DbSet<PostReport> PostReports { get; set; } = null!;
        public DbSet<CommentReport> CommentReports { get; set; } = null!;
        public DbSet<PostLike> PostLikes { get; set; } = null!;
        public DbSet<CommentLike> CommentLikes { get; set; } = null!;
        public DbSet<FileSignature> FileSignatures { get; set; } = null!;
        public DbSet<BlockedIp> BlockedIps { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;
        public DbSet<SearchHistory> SearchHistories { get; set; } = null!;
        public DbSet<SearchSuggestion> SearchSuggestions { get; set; } = null!;
        public DbSet<Synonym> Synonyms { get; set; } = null!;

        // 安全相关的 DbSet
        public DbSet<SecurityLog> SecurityLogs { get; set; } = null!;
        public DbSet<SecurityAuditLog> SecurityAuditLogs { get; set; } = null!;
        public DbSet<SecurityConfig> SecurityConfigs { get; set; } = null!;
        public DbSet<SpamPattern> SpamPatterns { get; set; } = null!;
        public DbSet<AccessLog> AccessLogs { get; set; } = null!;
        public DbSet<SensitiveWord> SensitiveWords { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // User
            builder.Entity<User>(b =>
            {
                b.ToTable("Users");
                b.Property(u => u.Bio).IsRequired(false);
                b.Property(u => u.Company).IsRequired(false);
                b.Property(u => u.Title).IsRequired(false);
                b.Property(u => u.Location).IsRequired(false);
                b.Property(u => u.Website).IsRequired(false);
                b.Property(u => u.Nickname).IsRequired(false);
                b.HasMany(u => u.Posts)
                    .WithOne(p => p.Author)
                    .HasForeignKey(p => p.AuthorId)
                    .OnDelete(DeleteBehavior.NoAction);

                b.HasMany(u => u.Comments)
                    .WithOne(c => c.Author)
                    .HasForeignKey(c => c.AuthorId)
                    .OnDelete(DeleteBehavior.NoAction);

                b.HasMany(u => u.CollectedPosts)
                    .WithMany(p => p.CollectedUsers)
                    .UsingEntity<Collection>(
                        j => j.HasOne<Post>().WithMany().HasForeignKey(c => c.PostId).OnDelete(DeleteBehavior.NoAction),
                        j => j.HasOne<User>().WithMany().HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.NoAction),
                        j =>
                        {
                            j.HasKey(c => new { c.UserId, c.PostId });
                            j.Property(c => c.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                            j.ToTable("Collections");
                        }
                    );

                b.HasMany(u => u.LikedPosts)
                    .WithMany(p => p.LikedUsers)
                    .UsingEntity<PostLike>(
                        j => j.HasOne<Post>().WithMany().HasForeignKey(pl => pl.PostId).OnDelete(DeleteBehavior.NoAction),
                        j => j.HasOne<User>().WithMany().HasForeignKey(pl => pl.UserId).OnDelete(DeleteBehavior.NoAction),
                        j =>
                        {
                            j.ToTable("PostLikes");
                            j.HasKey(pl => new { pl.UserId, pl.PostId });
                            j.Property(pl => pl.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                            j.Ignore(pl => pl.PostId1);
                            j.Ignore(pl => pl.UserId1);
                        }
                    );

                b.HasMany(u => u.LikedComments)
                    .WithMany(c => c.LikedUsers)
                    .UsingEntity<CommentLike>(
                        j => j.HasOne<Comment>().WithMany().HasForeignKey(cl => cl.CommentId).OnDelete(DeleteBehavior.NoAction),
                        j => j.HasOne<User>().WithMany().HasForeignKey(cl => cl.UserId).OnDelete(DeleteBehavior.NoAction),
                        j =>
                        {
                            j.ToTable("CommentLikes");
                            j.HasKey(cl => new { cl.UserId, cl.CommentId });
                            j.Property(cl => cl.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                            j.Ignore(cl => cl.CommentId1);
                            j.Ignore(cl => cl.UserId1);
                        }
                    );

                b.HasMany(u => u.FollowedTopics)
                    .WithMany(t => t.Followers)
                    .UsingEntity(j => j.ToTable("UserTopics"));

                b.HasMany(u => u.Followers)
                    .WithMany(u => u.Following)
                    .UsingEntity(j => j.ToTable("UserFollows"));
            });

            // Post
            builder.Entity<Post>(b =>
            {
                b.ToTable("Posts");
                b.HasMany(p => p.Comments)
                    .WithOne(c => c.Post)
                    .HasForeignKey(c => c.PostId)
                    .OnDelete(DeleteBehavior.NoAction);

                b.HasMany(p => p.Topics)
                    .WithMany(t => t.Posts)
                    .UsingEntity(j => j.ToTable("PostTopics"));

                b.HasMany(p => p.Media)
                    .WithMany(m => m.Posts)
                    .UsingEntity(j => j.ToTable("PostMedia"));
            });

            // Comment
            builder.Entity<Comment>(b =>
            {
                b.ToTable("Comments");
                b.HasMany(c => c.Replies)
                    .WithOne(c => c.Parent)
                    .HasForeignKey(c => c.ParentId)
                    .OnDelete(DeleteBehavior.NoAction);

                b.HasMany(c => c.Media)
                    .WithMany(m => m.Comments)
                    .UsingEntity(j => j.ToTable("CommentMedia"));
            });

            // Topic
            builder.Entity<Topic>(b =>
            {
                b.ToTable("Topics");
                b.HasMany(t => t.Children)
                    .WithOne(t => t.Parent)
                    .HasForeignKey(t => t.ParentId)
                    .OnDelete(DeleteBehavior.NoAction);

                b.HasOne(t => t.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(t => t.CreatedByUserId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // Media
            builder.Entity<Media>(b =>
            {
                b.ToTable("Media");
                b.HasOne(m => m.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(m => m.CreatedByUserId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // Report
            builder.Entity<Report>(b =>
            {
                b.ToTable("Reports");
                b.HasOne(r => r.Reporter)
                    .WithMany()
                    .HasForeignKey(r => r.ReporterId)
                    .OnDelete(DeleteBehavior.NoAction);

                b.HasOne(r => r.ProcessedByUser)
                    .WithMany()
                    .HasForeignKey(r => r.ProcessedByUserId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // PostReport
            builder.Entity<PostReport>(b =>
            {
                b.ToTable("PostReports");
                b.HasOne(r => r.Post)
                    .WithMany()
                    .HasForeignKey(r => r.PostId)
                    .OnDelete(DeleteBehavior.NoAction);

                b.HasOne(r => r.Reporter)
                    .WithMany()
                    .HasForeignKey(r => r.ReporterId)
                    .OnDelete(DeleteBehavior.NoAction);

                b.HasOne(r => r.ProcessedByUser)
                    .WithMany()
                    .HasForeignKey(r => r.ProcessedByUserId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // CommentReport
            builder.Entity<CommentReport>(b =>
            {
                b.ToTable("CommentReports");
                b.HasOne(r => r.Comment)
                    .WithMany(c => c.Reports)
                    .HasForeignKey(r => r.CommentId)
                    .OnDelete(DeleteBehavior.NoAction);

                b.HasOne(r => r.Reporter)
                    .WithMany()
                    .HasForeignKey(r => r.ReporterId)
                    .OnDelete(DeleteBehavior.NoAction);

                b.HasOne(r => r.ProcessedByUser)
                    .WithMany()
                    .HasForeignKey(r => r.ProcessedByUserId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // FileSignature
            builder.Entity<FileSignature>(b =>
            {
                b.ToTable("FileSignatures");
                b.HasOne(f => f.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(f => f.CreatedByUserId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // BlockedIp
            builder.Entity<BlockedIp>(b =>
            {
                b.ToTable("BlockedIps");
                b.HasOne(b => b.BlockedByUser)
                    .WithMany()
                    .HasForeignKey(b => b.BlockedByUserId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // Notification
            builder.Entity<Notification>(b =>
            {
                b.ToTable("Notifications");
                b.HasOne(n => n.User)
                    .WithMany()
                    .HasForeignKey(n => n.UserId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // SearchHistory
            builder.Entity<SearchHistory>(b =>
            {
                b.ToTable("SearchHistories");
                b.HasOne(s => s.User)
                    .WithMany()
                    .HasForeignKey(s => s.UserId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // Collection
            builder.Entity<Collection>(b =>
            {
                b.ToTable("Collections");
                b.HasOne(c => c.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(c => c.CreatedByUserId)
                    .OnDelete(DeleteBehavior.NoAction);

                b.HasOne(c => c.User)
                    .WithMany()
                    .HasForeignKey(c => c.UserId)
                    .OnDelete(DeleteBehavior.NoAction);

                b.HasOne(c => c.Post)
                    .WithMany()
                    .HasForeignKey(c => c.PostId)
                    .OnDelete(DeleteBehavior.NoAction);

                b.HasMany(c => c.Posts)
                    .WithMany()
                    .UsingEntity(j => j.ToTable("CollectionPosts"));

                b.HasMany(c => c.Followers)
                    .WithMany()
                    .UsingEntity(j => j.ToTable("CollectionFollowers"));
            });

            // 添加索引
            builder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            builder.Entity<Topic>()
                .HasIndex(t => t.Name)
                .IsUnique();

            builder.Entity<SearchHistory>()
                .HasIndex(sh => sh.Keyword);

            builder.Entity<SearchHistory>()
                .HasIndex(sh => sh.LastSearchedAt);

            // 添加 PostLike 和 CommentLike 的索引
            builder.Entity<PostLike>()
                .HasIndex(pl => new { pl.UserId, pl.PostId })
                .IsUnique();

            builder.Entity<CommentLike>()
                .HasIndex(cl => new { cl.UserId, cl.CommentId })
                .IsUnique();
        }
    }
} 