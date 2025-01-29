using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ZhihuClone.Core.Interfaces.Security;

namespace ZhihuClone.Web.Pages.Admin.Security
{
    [Authorize(Roles = "Admin")]
    public class DashboardModel : PageModel
    {
        private readonly IAccessLogRepository _accessLogRepository;
        private readonly IBlockedIpRepository _blockedIpRepository;
        private readonly ISensitiveWordRepository _sensitiveWordRepository;
        private readonly ISpamPatternRepository _spamPatternRepository;

        public DashboardModel(
            IAccessLogRepository accessLogRepository,
            IBlockedIpRepository blockedIpRepository,
            ISensitiveWordRepository sensitiveWordRepository,
            ISpamPatternRepository spamPatternRepository)
        {
            _accessLogRepository = accessLogRepository;
            _blockedIpRepository = blockedIpRepository;
            _sensitiveWordRepository = sensitiveWordRepository;
            _spamPatternRepository = spamPatternRepository;

            // 初始化列表属性
            TrafficLabels = new List<string>();
            NormalTraffic = new List<int>();
            SuspiciousTraffic = new List<int>();
            ThreatLabels = new List<string>();
            ThreatData = new List<int>();
            LatestEvents = new List<SecurityEvent>();
            SystemStatus = "未知";
        }

        public int OnlineUsers { get; set; }
        public int OnlineUsersChange { get; set; }
        public int BlockedIps { get; set; }
        public int NewBlockedIps { get; set; }
        public int SensitiveContentCount { get; set; }
        public int PendingSensitiveContent { get; set; }
        public int SystemHealth { get; set; }
        public string SystemStatus { get; set; }

        public List<string> TrafficLabels { get; set; }
        public List<int> NormalTraffic { get; set; }
        public List<int> SuspiciousTraffic { get; set; }

        public List<string> ThreatLabels { get; set; }
        public List<int> ThreatData { get; set; }

        public List<SecurityEvent> LatestEvents { get; set; }

        public async Task OnGetAsync()
        {
            // 获取统计数据
            var now = DateTime.UtcNow;
            var yesterday = now.AddDays(-1);
            var lastHour = now.AddHours(-1);
            var last24Hours = now.AddHours(-24);

            // 在线用户统计
            OnlineUsers = await GetOnlineUsersCount();
            OnlineUsersChange = await GetOnlineUsersChange();

            // IP封禁统计
            BlockedIps = await _blockedIpRepository.GetActiveBlockCountAsync();
            var newBlocks = await _blockedIpRepository.GetActiveBlocksAsync();
            NewBlockedIps = newBlocks.Count(x => x.CreatedAt >= last24Hours);

            // 敏感内容统计
            var sensitiveContent = await GetSensitiveContentStats();
            SensitiveContentCount = sensitiveContent.Total;
            PendingSensitiveContent = sensitiveContent.Pending;

            // 系统健康度
            var healthStats = await GetSystemHealthStats();
            SystemHealth = healthStats.Score;
            SystemStatus = healthStats.Status;

            // 流量数据（最近24小时，每小时一个点）
            TrafficLabels = new List<string>();
            NormalTraffic = new List<int>();
            SuspiciousTraffic = new List<int>();

            for (int i = 23; i >= 0; i--)
            {
                var time = now.AddHours(-i);
                TrafficLabels.Add(time.ToString("HH:00"));
                var hourStats = await GetTrafficStats(time);
                NormalTraffic.Add(hourStats.Normal);
                SuspiciousTraffic.Add(hourStats.Suspicious);
            }

            // 威胁分布
            var threats = await GetThreatDistribution();
            ThreatLabels = threats.Select(t => t.Type).ToList();
            ThreatData = threats.Select(t => t.Count).ToList();

            // 最新事件
            LatestEvents = await GetLatestEvents();
        }

        private async Task<int> GetOnlineUsersCount()
        {
            var lastFiveMinutes = DateTime.UtcNow.AddMinutes(-5);
            var recentLogs = await _accessLogRepository.GetByActionAsync("heartbeat");
            return recentLogs.Count(x => x.CreatedAt >= lastFiveMinutes);
        }

        private async Task<int> GetOnlineUsersChange()
        {
            var now = DateTime.UtcNow;
            var currentHour = await GetOnlineUsersCount();
            var lastHour = (await _accessLogRepository.GetByActionAsync("heartbeat"))
                .Count(x => x.CreatedAt >= now.AddHours(-1) && x.CreatedAt < now);

            if (lastHour == 0) return 100;
            return (int)((currentHour - lastHour) / (float)lastHour * 100);
        }

        private async Task<(int Total, int Pending)> GetSensitiveContentStats()
        {
            var last24Hours = DateTime.UtcNow.AddHours(-24);
            var logs = await _accessLogRepository.GetFailedAttemptsAsync(last24Hours, DateTime.UtcNow);
            var total = logs.Count(x => x.Action == "content_check");
            var pending = logs.Count(x => x.Action == "content_check" && x.Status == "pending");
            return (total, pending);
        }

        private async Task<(int Score, string Status)> GetSystemHealthStats()
        {
            var last24Hours = DateTime.UtcNow.AddHours(-24);
            var logs = await _accessLogRepository.GetFailedAttemptsAsync(last24Hours, DateTime.UtcNow);
            var totalRequests = await _accessLogRepository.CountAsync();
            var failureRate = totalRequests == 0 ? 0 : (logs.Count() / (float)totalRequests * 100);
            var score = 100 - (int)failureRate;

            var status = score switch
            {
                >= 90 => "系统运行良好",
                >= 70 => "系统运行正常",
                >= 50 => "需要关注",
                _ => "系统异常"
            };

            return (score, status);
        }

        private async Task<(int Normal, int Suspicious)> GetTrafficStats(DateTime hour)
        {
            var nextHour = hour.AddHours(1);
            var logs = await _accessLogRepository.GetFailedAttemptsAsync(hour, nextHour);
            var normal = logs.Count(x => x.IsSuccess);
            var suspicious = logs.Count(x => !x.IsSuccess);
            return (normal, suspicious);
        }

        private async Task<List<(string Type, int Count)>> GetThreatDistribution()
        {
            var last24Hours = DateTime.UtcNow.AddHours(-24);
            var logs = await _accessLogRepository.GetFailedAttemptsAsync(last24Hours, DateTime.UtcNow);
            
            return logs
                .GroupBy(x => x.Action)
                .Select(g => (Type: GetThreatType(g.Key), Count: g.Count()))
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToList();
        }

        private string GetThreatType(string action) => action switch
        {
            "login_failed" => "登录失败",
            "spam_detected" => "垃圾信息",
            "sensitive_content" => "敏感内容",
            "malware_detected" => "恶意软件",
            "rate_limit" => "频率限制",
            _ => "其他威胁"
        };

        private async Task<List<SecurityEvent>> GetLatestEvents()
        {
            var last24Hours = DateTime.UtcNow.AddHours(-24);
            var logs = await _accessLogRepository.GetFailedAttemptsAsync(last24Hours, DateTime.UtcNow);
            
            return logs
                .OrderByDescending(x => x.CreatedAt)
                .Take(10)
                .Select(x => new SecurityEvent
                {
                    Id = x.Id,
                    Time = x.CreatedAt,
                    Type = GetEventType(x.Action),
                    TypeColor = GetEventColor(x.Action),
                    IpAddress = x.IpAddress,
                    Status = x.IsSuccess ? "成功" : "失败"
                })
                .ToList();
        }

        private string GetEventType(string action) => action switch
        {
            "login_failed" => "登录尝试",
            "spam_detected" => "垃圾信息",
            "sensitive_content" => "敏感内容",
            "malware_detected" => "恶意软件",
            "rate_limit" => "频率限制",
            _ => "其他事件"
        };

        private string GetEventColor(string action) => action switch
        {
            "login_failed" => "danger",
            "spam_detected" => "warning",
            "sensitive_content" => "info",
            "malware_detected" => "dark",
            "rate_limit" => "secondary",
            _ => "primary"
        };
    }

    public class SecurityEvent
    {
        public int Id { get; set; }
        public DateTime Time { get; set; }
        public required string Type { get; set; }
        public required string TypeColor { get; set; }
        public required string IpAddress { get; set; }
        public required string Status { get; set; }
    }
} 