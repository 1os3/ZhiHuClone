using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using ZhihuClone.Core.Interfaces.Security;
using ZhihuClone.Core.Models;
using ZhihuClone.Core.Models.Security;
using Microsoft.Extensions.Logging;
using ZhihuClone.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using ZhihuClone.Infrastructure.Data;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;

namespace ZhihuClone.Infrastructure.Security;

public class FirewallService : IFirewallService
{
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;
    private readonly Dictionary<string, int> _rateLimits;
    private readonly TimeSpan _rateLimitDuration = TimeSpan.FromMinutes(1);
    private readonly List<string> _suspiciousPatterns;
    private readonly List<string> _spamPatterns;
    private readonly IBlockedIpRepository _blockedIpRepository;
    private readonly ISensitiveWordRepository _sensitiveWordRepository;
    private readonly ISpamPatternRepository _spamPatternRepository;
    private readonly IFileSignatureRepository _fileSignatureRepository;
    private readonly IAccessLogRepository _accessLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<FirewallService> _logger;
    private readonly ApplicationDbContext _context;
    private readonly Dictionary<string, int> _ipFailedAttempts;
    private const int MaxStringLength = 10000; // 最大字符串长度
    private const int MaxFailedAttempts = 5; // 最大失败尝试次数
    private readonly TimeSpan BlockDuration = TimeSpan.FromMinutes(15); // 封禁时长
    private readonly Dictionary<string, string[]> _allowedFileTypes = new Dictionary<string, string[]>
    {
        { "image/jpeg", new[] { ".jpg", ".jpeg", "FFD8FF" } },
        { "image/png", new[] { ".png", "89504E47" } },
        { "image/gif", new[] { ".gif", "47494638" } },
        { "image/webp", new[] { ".webp", "52494646" } },
        { "image/svg+xml", new[] { ".svg", "3C737667" } },
        { "application/pdf", new[] { ".pdf", "25504446" } },
        { "text/plain", new[] { ".txt", ".log", ".ini" } },
        { "text/html", new[] { ".html", ".htm", "3C21444F" } },
        { "text/css", new[] { ".css" } },
        { "text/javascript", new[] { ".js" } },
        { "application/json", new[] { ".json" } },
        { "application/xml", new[] { ".xml", "3C3F786D" } },
        { "application/zip", new[] { ".zip", "504B0304" } },
        { "application/x-rar-compressed", new[] { ".rar", "526172" } },
        { "application/x-7z-compressed", new[] { ".7z", "377ABCAF" } },
        { "audio/mpeg", new[] { ".mp3", "494433" } },
        { "audio/wav", new[] { ".wav", "52494646" } },
        { "video/mp4", new[] { ".mp4", "66747970" } },
        { "video/webm", new[] { ".webm", "1A45DFA3" } },
        { "video/x-matroska", new[] { ".mkv", "1A45DFA3" } }
    };
    private readonly Dictionary<string, long> _maxFileSizes = new Dictionary<string, long>
    {
        { "image/", 10 * 1024 * 1024 },      // 10MB for images
        { "video/", 500 * 1024 * 1024 },     // 500MB for videos
        { "audio/", 50 * 1024 * 1024 },      // 50MB for audio
        { "application/pdf", 20 * 1024 * 1024 }, // 20MB for PDFs
        { "text/", 5 * 1024 * 1024 },        // 5MB for text files
        { "application/zip", 100 * 1024 * 1024 }, // 100MB for archives
        { "default", 50 * 1024 * 1024 }      // 50MB default
    };
    private readonly HashSet<string> _whitelistedIps;
    private readonly HashSet<string> _trustedUserAgents;

    public FirewallService(
        IMemoryCache cache,
        IConfiguration configuration,
        IBlockedIpRepository blockedIpRepository,
        ISensitiveWordRepository sensitiveWordRepository,
        ISpamPatternRepository spamPatternRepository,
        IFileSignatureRepository fileSignatureRepository,
        IAccessLogRepository accessLogRepository,
        IUnitOfWork unitOfWork,
        ILogger<FirewallService> logger,
        ApplicationDbContext context)
    {
        _cache = cache;
        _configuration = configuration;
        
        // 配置不同操作的速率限制
        _rateLimits = new Dictionary<string, int>
        {
            { "login", 5 },
            { "register", 2 },
            { "post", 10 },
            { "comment", 20 },
            { "like", 30 },
            { "search", 60 },
            { "api", 100 }
        };

        // 配置可疑内容模式
        _suspiciousPatterns = new List<string>
        {
            @"<script.*?>.*?</script>",
            @"javascript:",
            @"vbscript:",
            @"onload=",
            @"onerror=",
            @"onclick=",
            @"onmouseover=",
            @"onfocus=",
            @"onblur=",
            @"eval\(",
            @"document\.cookie",
            @"document\.write",
            @"\.\.\/",
            @"\.\.\\",
            @"\/etc\/passwd",
            @"cmd\.exe",
            @"bash\.exe",
            @"SELECT.*FROM",
            @"UNION.*SELECT",
            @"DROP.*TABLE",
            @"ALTER.*TABLE",
            @"DELETE.*FROM",
            @"UPDATE.*SET"
        };

        // 配置垃圾信息模式
        _spamPatterns = new List<string>
        {
            @"(viagra|cialis)",
            @"(casino|gambling|bet)",
            @"(free.*?money|make.*?money)",
            @"(\$\d+.*?hour|\d+\$.*?day)",
            @"(work.*?home|earn.*?home)",
            @"(crypto|bitcoin|ethereum)",
            @"(investment.*?opportunity)",
            @"(lottery|jackpot|prize.*?won)",
            @"(weight.*?loss|diet.*?pill)",
            @"(replica.*?watch|fake.*?brand)"
        };

        _blockedIpRepository = blockedIpRepository;
        _sensitiveWordRepository = sensitiveWordRepository;
        _spamPatternRepository = spamPatternRepository;
        _fileSignatureRepository = fileSignatureRepository;
        _accessLogRepository = accessLogRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _context = context;
        _ipFailedAttempts = new Dictionary<string, int>();

        // 配置白名单IP
        _whitelistedIps = new HashSet<string>(
            configuration.GetSection("Security:WhitelistedIps").Get<string[]>() ?? Array.Empty<string>()
        );

        // 配置可信任的User-Agent
        _trustedUserAgents = new HashSet<string>(
            configuration.GetSection("Security:TrustedUserAgents").Get<string[]>() ?? Array.Empty<string>()
        );
    }

    public async Task<bool> IsIpBlockedAsync(string ipAddress)
    {
        var blockedIp = await _blockedIpRepository.GetByIpAsync(ipAddress);
        return blockedIp != null && blockedIp.IsEnabled;
    }

    public async Task<bool> IsUserBlockedAsync(int userId)
    {
        return await Task.FromResult(_cache.TryGetValue($"blocked_user_{userId}", out _));
    }

    public async Task<bool> CheckRateLimitAsync(string ipAddress, string action)
    {
        if (!_rateLimits.ContainsKey(action))
            return true;

        var cacheKey = $"ratelimit:{action}:{ipAddress}";
        var counter = await Task.FromResult(_cache.GetOrCreate(cacheKey, entry =>
        {
            entry.SetAbsoluteExpiration(TimeSpan.FromMinutes(1));
            return 0;
        }));

        if (counter >= _rateLimits[action])
        {
            await LogRateLimitViolationAsync(ipAddress, action);
            return false;
        }

        _cache.Set(cacheKey, counter + 1, TimeSpan.FromMinutes(1));
        return true;
    }

    public async Task<bool> ValidateRequestAsync(HttpContext context)
    {
        try
        {
            var ipAddress = GetClientIpAddress(context);

            // 1. 检查IP白名单
            if (_whitelistedIps.Contains(ipAddress))
            {
                return true;
            }

            // 2. 检查IP是否被封禁
            if (await IsIpBlockedAsync(ipAddress))
            {
                _logger.LogWarning($"Blocked request from banned IP: {ipAddress}");
                return false;
            }

            // 3. 检查请求频率
            var path = context.Request.Path.Value?.ToLower();
            if (!string.IsNullOrEmpty(path))
            {
                string action = GetActionFromPath(path);
                if (!await CheckRateLimitAsync(ipAddress, action))
                {
                    _logger.LogWarning($"Rate limit exceeded for IP {ipAddress} on action {action}");
                    return false;
                }
            }

            // 4. 检查请求头
            if (!ValidateRequestHeaders(context.Request.Headers))
            {
                _logger.LogWarning($"Invalid request headers from IP: {ipAddress}");
                return false;
            }

            // 5. 检查请求体
            if (context.Request.Method == "POST" || context.Request.Method == "PUT")
            {
                if (!await ValidateRequestBodyAsync(context.Request))
                {
                    _logger.LogWarning($"Invalid request body from IP: {ipAddress}");
                    return false;
                }
            }

            // 6. 检查请求URL
            if (!ValidateRequestUrl(context.Request))
            {
                _logger.LogWarning($"Invalid request URL from IP: {ipAddress}");
                return false;
            }

            // 7. 检查文件上传
            if (context.Request.HasFormContentType && context.Request.Form.Files.Any())
            {
                if (!await ValidateFileUploadsAsync(context.Request.Form.Files))
                {
                    _logger.LogWarning($"Invalid file upload from IP: {ipAddress}");
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating request");
            return false;
        }
    }

    private string GetActionFromPath(string path)
    {
        if (path.Contains("/login")) return "login";
        if (path.Contains("/register")) return "register";
        if (path.Contains("/post")) return "post";
        if (path.Contains("/comment")) return "comment";
        if (path.Contains("/like")) return "like";
        if (path.Contains("/search")) return "search";
        if (path.StartsWith("/api/")) return "api";
        return "default";
    }

    private bool ValidateRequestHeaders(IHeaderDictionary headers)
    {
        try
        {
            // 1. 检查必需的请求头
            if (!headers.ContainsKey("User-Agent") || 
                string.IsNullOrWhiteSpace(headers["User-Agent"]))
            {
                return false;
            }

            // 2. 检查User-Agent
            var userAgent = headers["User-Agent"].ToString().ToLower();
            if (!_trustedUserAgents.Any(ua => userAgent.Contains(ua.ToLower())))
            {
                if (userAgent.Contains("curl") || userAgent.Contains("wget") || 
                    userAgent.Contains("python") || userAgent.Contains("java"))
                {
                    if (!headers.ContainsKey("Accept") || 
                        !headers.ContainsKey("Accept-Language") || 
                        !headers.ContainsKey("Accept-Encoding"))
                    {
                        return false;
                    }
                }
            }

            // 3. 检查Origin和Referer
            if (headers.ContainsKey("Origin") || headers.ContainsKey("Referer"))
            {
                var origin = headers["Origin"].ToString();
                var referer = headers["Referer"].ToString();
                var allowedDomains = _configuration.GetSection("Security:AllowedDomains")
                    .Get<string[]>() ?? Array.Empty<string>();

                if (!string.IsNullOrEmpty(origin) && 
                    !allowedDomains.Any(domain => origin.Contains(domain)))
                {
                    return false;
                }

                if (!string.IsNullOrEmpty(referer) && 
                    !allowedDomains.Any(domain => referer.Contains(domain)))
                {
                    return false;
                }
            }

            // 4. 检查XSS攻击特征
            if (headers.Any(h => h.Value.ToString().Contains("<script") || 
                               h.Value.ToString().Contains("javascript:") ||
                               h.Value.ToString().Contains("data:")))
            {
                return false;
            }

            // 5. 检查SQL注入特征
            if (headers.Any(h => h.Value.ToString().Contains("'") || 
                               h.Value.ToString().Contains("--") || 
                               h.Value.ToString().Contains(";")))
            {
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating request headers");
            return false;
        }
    }

    private async Task<bool> ValidateRequestBodyAsync(HttpRequest request)
    {
        try
        {
            if (request.ContentLength > _configuration.GetValue<long>("Security:MaxRequestBodySize", 10485760))
            {
                return false;
            }

            if (request.ContentType?.ToLower().Contains("application/json") == true)
            {
                request.EnableBuffering();
                using var reader = new StreamReader(request.Body, leaveOpen: true);
                var body = await reader.ReadToEndAsync();
                request.Body.Position = 0;

                // 检查XSS攻击
                if (_suspiciousPatterns.Any(pattern => Regex.IsMatch(body, pattern)))
                {
                    return false;
                }

                // 检查垃圾信息
                if (_spamPatterns.Any(pattern => Regex.IsMatch(body, pattern)))
                {
                    return false;
                }

                // 检查敏感词
                var sensitiveWords = await _sensitiveWordRepository.GetAllAsync();
                if (sensitiveWords.Any(word => body.Contains(word.Word)))
                {
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating request body");
            return false;
        }
    }

    private bool ValidateRequestUrl(HttpRequest request)
    {
        try
        {
            var url = request.Path.Value + request.QueryString.Value;

            // 检查URL长度
            if (url.Length > _configuration.GetValue<int>("Security:MaxUrlLength", 2000))
            {
                return false;
            }

            // 检查非法字符
            if (url.Contains("..") || url.Contains("//") || url.Contains("\\"))
            {
                return false;
            }

            // 检查XSS攻击
            if (_suspiciousPatterns.Any(pattern => Regex.IsMatch(url, pattern)))
            {
                return false;
            }

            // 检查SQL注入
            if (url.Contains("'") || url.Contains("--") || url.Contains(";"))
            {
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating request URL");
            return false;
        }
    }

    private async Task<bool> ValidateFileUploadsAsync(IFormFileCollection files)
    {
        try
        {
            foreach (var file in files)
            {
                // 检查文件大小
                if (file.Length > _configuration.GetValue<long>("Security:MaxFileSize", 5242880))
                {
                    return false;
                }

                // 检查文件类型
                var allowedTypes = _configuration.GetSection("Security:AllowedFileTypes")
                    .Get<string[]>() ?? Array.Empty<string>();
                if (!allowedTypes.Contains(file.ContentType.ToLower()))
                {
                    return false;
                }

                // 检查文件签名
                using var stream = file.OpenReadStream();
                var buffer = new byte[Math.Min(file.Length, 8192)];
                var memory = new Memory<byte>(buffer);
                var bytesRead = await stream.ReadAsync(memory);
                var signature = Convert.ToBase64String(buffer.AsSpan(0, Math.Min(bytesRead, 16)).ToArray());

                if (!ValidateFileSignature(signature, file.ContentType))
                {
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating file uploads");
            return false;
        }
    }

    private bool ValidateFileSignature(string signature, string contentType)
    {
        // 实现文件签名验证逻辑
        return true;
    }

    private async Task LogRateLimitViolationAsync(string ipAddress, string action)
    {
        var cacheKey = $"ratelimit:violations:{ipAddress}";
        var violations = _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.SetAbsoluteExpiration(TimeSpan.FromHours(1));
            return 0;
        });

        _cache.Set(cacheKey, violations + 1, TimeSpan.FromHours(1));

        if (violations >= _configuration.GetValue<int>("Security:MaxViolationsBeforeBan", 10))
        {
            await BlockIpAsync(ipAddress, $"Rate limit violations: {action}");
        }
    }

    private string GetClientIpAddress(HttpContext context)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    public async Task BlockIpAsync(string ipAddress, string reason)
    {
        var blockedIp = await _blockedIpRepository.GetByIpAsync(ipAddress);
        if (blockedIp == null)
        {
            blockedIp = new BlockedIp
            {
                IpAddress = ipAddress,
                Reason = reason,
                BlockedAt = DateTime.UtcNow,
                IsEnabled = true
            };
            await _blockedIpRepository.AddAsync(blockedIp);
        }
        else
        {
            blockedIp.IsEnabled = true;
            blockedIp.UnblockedAt = null;
            blockedIp.Reason = reason;
            blockedIp.BlockedAt = DateTime.UtcNow;
            await _blockedIpRepository.UpdateAsync(blockedIp);
        }
        await _unitOfWork.SaveChangesAsync();
    }

    public void BlockUser(int userId, string reason)
    {
        var blockDuration = TimeSpan.FromDays(7);
        _cache.Set($"blocked_user_{userId}", reason, blockDuration);
    }

    public async Task UnblockIpAsync(string ipAddress)
    {
        var blockedIp = await _blockedIpRepository.GetByIpAsync(ipAddress);
        if (blockedIp != null)
        {
            blockedIp.IsEnabled = false;
            blockedIp.UnblockedAt = DateTime.UtcNow;
            await _blockedIpRepository.UpdateAsync(blockedIp);
            await _unitOfWork.SaveChangesAsync();
        }
    }

    public async Task UnblockUserAsync(int userId)
    {
        await Task.Run(() => _cache.Remove($"blocked_user_{userId}"));
    }

    public async Task LogAccessAsync(string ipAddress, string action, bool isSuccess)
    {
        await _accessLogRepository.AddAsync(new AccessLog
        {
            IpAddress = ipAddress,
            Action = action,
            IsSuccess = isSuccess,
            CreatedAt = DateTime.UtcNow
        });
    }

    public async Task<bool> IsSuspiciousContentAsync(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return false;

        return await Task.Run(() => _suspiciousPatterns.Any(pattern => 
            Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase)));
    }

    public async Task<bool> IsSpamAsync(string content)
    {
        try
        {
            // 1. 检查垃圾信息模式
            foreach (var pattern in _spamPatterns)
            {
                if (Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase))
                {
                    _logger.LogWarning($"Spam pattern detected: {pattern}");
                    return true;
                }
            }

            // 2. 检查数据库中的垃圾信息模式
            var dbPatterns = await _context.SpamPatterns
                .Where(p => p.IsEnabled)
                .Select(p => p.Pattern)
                .ToListAsync();

            foreach (var pattern in dbPatterns)
            {
                if (Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase))
                {
                    _logger.LogWarning($"Database spam pattern detected: {pattern}");
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking spam content");
            return true; // 出错时默认阻止
        }
    }

    public async Task<bool> ContainsSensitiveContentAsync(string content)
    {
        try
        {
            var sensitiveWords = await _sensitiveWordRepository.GetAllAsync();
            return sensitiveWords.Any(word => content.Contains(word.Word));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking sensitive content");
            return true; // 出错时默认阻止
        }
    }

    public async Task<bool> IsSpamContentAsync(string content)
    {
        var spamPatterns = await _spamPatternRepository.GetAllAsync();
        return spamPatterns.Any(pattern => content.Contains(pattern.Pattern));
    }

    public async Task<(bool isClean, string threatName)> ScanFileForVirus(IFormFile file)
    {
        try
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var fileContent = ms.ToArray();

            // 1. 基本文件检查
            if (fileContent.Length == 0)
            {
                return (false, "EmptyFile");
            }

            if (fileContent.Length > 50 * 1024 * 1024) // 50MB
            {
                return (false, "FileTooLarge");
            }

            // 2. 文件签名检查
            var signature = await GetFileSignatureAsync(file);
            if (await IsFileSignatureBlockedAsync(signature))
            {
                return (false, "BlockedFileType");
            }

            // 3. 病毒扫描
            if (!await ScanForVirusAsync(fileContent))
            {
                return (false, "VirusDetected");
            }

            // 4. 恶意代码检查
            if (await ContainsMaliciousCode(fileContent))
            {
                return (false, "MaliciousCode");
            }

            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error scanning file: {file.FileName}");
            return (false, "ScanError");
        }
    }

    private async Task<bool> ContainsMaliciousCode(byte[] fileContent)
    {
        try
        {
            // 1. 检查文件头部特征
            var fileHeader = fileContent.Take(4).ToArray();
            var suspiciousHeaders = new byte[][]
            {
                new byte[] { 0x4D, 0x5A }, // EXE
                new byte[] { 0x7F, 0x45, 0x4C, 0x46 }, // ELF
                new byte[] { 0xCA, 0xFE, 0xBA, 0xBE }, // Java class
                new byte[] { 0x50, 0x4B, 0x03, 0x04 }  // ZIP, JAR
            };

            if (suspiciousHeaders.Any(header => 
                fileContent.Length >= header.Length && 
                fileContent.Take(header.Length).SequenceEqual(header)))
            {
                return true;
            }

            // 2. 检查常见的恶意代码特征
            var maliciousPatterns = new string[]
            {
                @"<script.*?>.*?</script>",
                @"javascript:",
                @"eval\(",
                @"document\.cookie",
                @"base64_decode",
                @"shell_exec",
                @"exec\(",
                @"system\("
            };

            var fileText = Encoding.UTF8.GetString(fileContent);
            foreach (var pattern in maliciousPatterns)
            {
                if (Regex.IsMatch(fileText, pattern, RegexOptions.IgnoreCase))
                {
                    return true;
                }
            }

            // 3. 检查文件熵值
            if (CalculateEntropy(fileContent) > 7.5)
            {
                return true;
            }

            // 4. 检查数据库中的恶意代码特征
            var dbPatterns = await _context.SpamPatterns
                .Where(p => p.IsEnabled && p.PatternType == "malicious_code")
                .Select(p => p.Pattern)
                .ToListAsync();

            foreach (var pattern in dbPatterns)
            {
                if (Regex.IsMatch(fileText, pattern, RegexOptions.IgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for malicious code");
            return true; // 出错时默认阻止
        }
    }

    public async Task<bool> ValidateFileContentType(IFormFile file, string expectedContentType)
    {
        try
        {
            // 1. 检查文件扩展名
            var extension = Path.GetExtension(file.FileName).ToLower();
            var allowedExtensions = new Dictionary<string, string[]>
            {
                { "image/jpeg", new[] { ".jpg", ".jpeg" } },
                { "image/png", new[] { ".png" } },
                { "image/gif", new[] { ".gif" } },
                { "application/pdf", new[] { ".pdf" } },
                { "text/plain", new[] { ".txt" } }
            };

            if (!allowedExtensions.TryGetValue(expectedContentType, out var validExtensions) ||
                !validExtensions.Contains(extension))
            {
                _logger.LogWarning($"Invalid file extension: {extension} for content type: {expectedContentType}");
                return false;
            }

            // 2. 检查文件头部签名
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var fileContent = ms.ToArray();
            var signature = await GetFileSignatureAsync(file);

            var validSignatures = await _context.FileSignatures
                .Where(s => s.FileType == expectedContentType && s.IsEnabled)
                .Select(s => s.Signature)
                .ToListAsync();

            if (!validSignatures.Contains(signature))
            {
                _logger.LogWarning($"Invalid file signature: {signature} for content type: {expectedContentType}");
                return false;
            }

            // 3. 检查文件内容
            if (expectedContentType.StartsWith("image/"))
            {
                using var image = Image.Load(fileContent);
                if (image == null)
                {
                    _logger.LogWarning("Invalid image file");
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error validating file content type: {file.FileName}");
            return false;
        }
    }

    public async Task<bool> ContainsXss(IFormFile file)
    {
        try
        {
            using var reader = new StreamReader(file.OpenReadStream());
            var content = await reader.ReadToEndAsync();

            // 1. 检查常见的XSS攻击向量
            var xssPatterns = new string[]
            {
                @"<script.*?>.*?</script>",
                @"javascript:",
                @"onload=",
                @"onerror=",
                @"onclick=",
                @"onmouseover=",
                @"onfocus=",
                @"onblur=",
                @"alert\(",
                @"prompt\(",
                @"confirm\(",
                @"eval\(",
                @"document\.cookie",
                @"document\.domain",
                @"document\.write",
                @"<iframe.*?>",
                @"<meta.*?>",
                @"<link.*?>",
                @"<style.*?>.*?</style>",
                @"expression\(",
                @"url\(",
                @"data:",
                @"vbscript:"
            };

            foreach (var pattern in xssPatterns)
            {
                if (Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase))
                {
                    _logger.LogWarning($"XSS pattern detected: {pattern}");
                    return true;
                }
            }

            // 2. 检查编码的XSS攻击
            var encodedContent = content.ToLower();
            var encodedPatterns = new string[]
            {
                @"%3cscript",
                @"%3c/script",
                @"&#x3c;script",
                @"&#x3c;/script",
                @"&lt;script",
                @"&lt;/script",
                @"\\x3cscript",
                @"\\x3c/script"
            };

            foreach (var pattern in encodedPatterns)
            {
                if (encodedContent.Contains(pattern))
                {
                    _logger.LogWarning($"Encoded XSS pattern detected: {pattern}");
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error checking XSS in file: {file.FileName}");
            return true; // 出错时默认阻止
        }
    }

    public async Task<bool> ContainsSqlInjection(IFormFile file)
    {
        try
        {
            using var reader = new StreamReader(file.OpenReadStream());
            var content = await reader.ReadToEndAsync();

            // 1. 检查常见的SQL注入模式
            var sqlPatterns = new string[]
            {
                @"(\s|^)SELECT.*FROM",
                @"(\s|^)INSERT.*INTO",
                @"(\s|^)UPDATE.*SET",
                @"(\s|^)DELETE.*FROM",
                @"(\s|^)DROP.*TABLE",
                @"(\s|^)ALTER.*TABLE",
                @"(\s|^)EXEC.*sp_",
                @"(\s|^)EXECUTE.*sp_",
                @"--",
                @";.*--",
                @"/\*.*\*/",
                @"'.*OR.*'.*'.*=.*'",
                @"'.*AND.*'.*'.*=.*'",
                @"UNION.*SELECT",
                @"UNION.*ALL.*SELECT",
                @"CONCAT.*\(",
                @"VERSION\(",
                @"DATABASE\(",
                @"USER\(",
                @"SYSTEM_USER",
                @"@@version",
                @"LOAD_FILE",
                @"BENCHMARK\(",
                @"SLEEP\("
            };

            foreach (var pattern in sqlPatterns)
            {
                if (Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase))
                {
                    _logger.LogWarning($"SQL injection pattern detected: {pattern}");
                    return true;
                }
            }

            // 2. 检查编码的SQL注入
            var encodedContent = content.ToLower();
            var encodedPatterns = new string[]
            {
                @"%27",      // '
                @"%22",      // "
                @"%2527",    // %27
                @"%252527",  // %2527
                @"char\(",
                @"convert\(",
                @"cast\("
            };

            foreach (var pattern in encodedPatterns)
            {
                if (encodedContent.Contains(pattern))
                {
                    _logger.LogWarning($"Encoded SQL injection pattern detected: {pattern}");
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error checking SQL injection in file: {file.FileName}");
            return true; // 出错时默认阻止
        }
    }

    public async Task<(bool isSafe, string threat)> ScanFileForMalware(IFormFile file)
    {
        // 实现恶意软件扫描逻辑
        return await Task.FromResult((true, string.Empty));
    }

    public async Task<bool> IsValidFileSignatureAsync(string signature)
    {
        var fileSignature = await _fileSignatureRepository.GetBySignatureAsync(signature);
        return fileSignature != null && fileSignature.IsEnabled;
    }

    public async Task<bool> IsBotRequest(string userAgent, string ipAddress)
    {
        try
        {
            if (string.IsNullOrEmpty(userAgent))
            {
                _logger.LogWarning($"Empty user agent from IP: {ipAddress}");
                return true;
            }

            // 1. 检查常见的机器人标识
            var botPatterns = new[]
            {
                "bot", "crawler", "spider", "slurp", "baiduspider",
                "yandex", "mediapartners-google", "ia_archiver",
                "semrushbot", "ahrefs", "mj12bot", "dotbot"
            };

            if (botPatterns.Any(p => userAgent.ToLower().Contains(p)))
            {
                _logger.LogWarning($"Bot pattern detected in user agent: {userAgent}");
                return true;
            }

            // 2. 检查请求频率
            var recentRequests = await _context.AccessLogs
                .Where(l => l.IpAddress == ipAddress && 
                           l.CreatedAt >= DateTime.UtcNow.AddMinutes(-1))
                .ToListAsync();

            if (recentRequests.Count > 60) // 每分钟超过60次请求
            {
                _logger.LogWarning($"High frequency requests from IP: {ipAddress}");
                return true;
            }

            // 3. 检查请求模式
            var requestPattern = recentRequests
                .GroupBy(r => r.Action)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            if (requestPattern != null && requestPattern.Count() > 30) // 同一操作重复超过30次
            {
                _logger.LogWarning($"Repetitive action pattern detected from IP: {ipAddress}");
                return true;
            }

            // 4. 检查IP信誉
            var reputationScore = await GetIpReputationScore(ipAddress);
            if (reputationScore < 30) // 信誉分数过低
            {
                _logger.LogWarning($"Low reputation score for IP: {ipAddress}");
                return true;
            }

            // 5. 检查浏览器特征
            if (!HasValidBrowserFeatures(userAgent))
            {
                _logger.LogWarning($"Invalid browser features from IP: {ipAddress}");
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error checking bot request from IP: {ipAddress}");
            return true; // 出错时默认阻止
        }
    }

    private bool HasValidBrowserFeatures(string userAgent)
    {
        try
        {
            // 1. 检查是否包含有效的浏览器标识
            var validBrowsers = new[]
            {
                "chrome", "firefox", "safari", "edge", "opera",
                "msie", "trident", "mozilla"
            };

            if (!validBrowsers.Any(b => userAgent.ToLower().Contains(b)))
            {
                return false;
            }

            // 2. 检查版本号格式
            if (!Regex.IsMatch(userAgent, @"[\d\.]+"))
            {
                return false;
            }

            // 3. 检查操作系统信息
            var validOS = new[]
            {
                "windows", "macintosh", "linux", "android",
                "iphone", "ipad", "ios"
            };

            if (!validOS.Any(os => userAgent.ToLower().Contains(os)))
            {
                return false;
            }

            // 4. 检查异常字符
            if (Regex.IsMatch(userAgent, @"[^\x20-\x7E]"))
            {
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating browser features");
            return false;
        }
    }

    public async Task<int> GetIpReputationScore(string ipAddress)
    {
        try
        {
            // 1. 检查历史违规记录
            var violations = await _context.BlockedIps
                .CountAsync(b => b.IpAddress == ipAddress);
            
            // 2. 检查最近的访问记录
            var recentLogs = await _context.AccessLogs
                .Where(l => l.IpAddress == ipAddress && 
                           l.CreatedAt >= DateTime.UtcNow.AddDays(-7))
                .ToListAsync();

            // 3. 计算基础分数（100分）
            int score = 100;

            // 4. 根据违规记录扣分
            score -= violations * 20; // 每次违规扣20分

            // 5. 检查访问频率
            var hourlyRequests = recentLogs
                .GroupBy(l => l.CreatedAt.Hour)
                .Select(g => g.Count())
                .DefaultIfEmpty(0)
                .Average();

            if (hourlyRequests > 1000) score -= 30; // 频率过高扣30分
            else if (hourlyRequests > 500) score -= 15; // 频率较高扣15分

            // 6. 检查失败率
            var failureRate = recentLogs.Count(l => !l.IsSuccess) * 100.0 / Math.Max(1, recentLogs.Count);
            if (failureRate > 50) score -= 20; // 失败率过高扣20分
            else if (failureRate > 20) score -= 10; // 失败率较高扣10分

            // 7. 检查可疑行为模式
            var suspiciousPatterns = recentLogs
                .GroupBy(l => l.Action)
                .Any(g => g.Count() > 100); // 同一操作重复超过100次

            if (suspiciousPatterns) score -= 15; // 发现可疑模式扣15分

            // 8. 确保分数在0-100之间
            return Math.Max(0, Math.Min(100, score));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error calculating reputation score for IP: {ipAddress}");
            return 0; // 出错时返回最低分
        }
    }

    public async Task<bool> IsFileSignatureBlockedAsync(string signature)
    {
        try
        {
            var fileSignature = await _context.FileSignatures
                .FirstOrDefaultAsync(s => s.Signature == signature);
                
            return fileSignature != null && fileSignature.IsBlocked;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error checking file signature block status: {signature}");
            return true; // 出错时默认阻止
        }
    }

    public async Task BlockFileSignatureAsync(string signature, string reason)
    {
        var fileSignature = await _fileSignatureRepository.GetBySignatureAsync(signature);
        if (fileSignature == null)
        {
            fileSignature = new FileSignature
            {
                Signature = signature,
                Reason = reason,
                IsBlocked = true,
                BlockedAt = DateTime.UtcNow
            };
            await _fileSignatureRepository.AddAsync(fileSignature);
        }
        else
        {
            fileSignature.IsBlocked = true;
            fileSignature.Reason = reason;
            fileSignature.BlockedAt = DateTime.UtcNow;
            _fileSignatureRepository.Update(fileSignature);
        }
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task UnblockFileSignatureAsync(string signature)
    {
        var fileSignature = await _fileSignatureRepository.GetBySignatureAsync(signature);
        if (fileSignature != null)
        {
            fileSignature.IsBlocked = false;
            fileSignature.UnblockedAt = DateTime.UtcNow;
            _fileSignatureRepository.Update(fileSignature);
            await _unitOfWork.SaveChangesAsync();
        }
    }

    public async Task<(bool isClean, string threatName)> ScanContentAsync(string content)
    {
        if (await IsSpamAsync(content))
        {
            return (false, "Spam");
        }

        if (await ContainsSensitiveContentAsync(content))
        {
            return (false, "SensitiveWords");
        }

        return (true, string.Empty);
    }

    public async Task<(bool isSafe, string threat)> ScanFileAsync(string signature)
    {
        if (await IsFileSignatureBlockedAsync(signature))
        {
            var fileSignature = await _fileSignatureRepository.GetBySignatureAsync(signature);
            return (false, fileSignature?.Reason ?? "Unknown");
        }

        return (true, string.Empty);
    }

    public async Task<bool> AddSpamPatternAsync(string pattern)
    {
        try
        {
            var spamPattern = new SpamPattern
            {
                Pattern = pattern,
                IsEnabled = true,
                CreatedAt = DateTime.UtcNow
            };
            await _spamPatternRepository.AddAsync(spamPattern);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding spam pattern: {Pattern}", pattern);
            return false;
        }
    }

    public async Task<bool> AddSensitiveWordAsync(string word)
    {
        try
        {
            var sensitiveWord = new SensitiveWord
            {
                Word = word,
                IsEnabled = true,
                CreatedAt = DateTime.UtcNow
            };
            await _sensitiveWordRepository.AddAsync(sensitiveWord);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding sensitive word: {Word}", word);
            return false;
        }
    }

    public async Task<bool> ValidateInputAsync(string input, string inputType)
    {
        try
        {
            // 1. 空值检查
            if (string.IsNullOrWhiteSpace(input))
            {
                _logger.LogWarning($"Empty {inputType} detected");
                return false;
            }

            // 2. 长度检查
            if (input.Length > MaxStringLength)
            {
                _logger.LogWarning($"Input {inputType} exceeds maximum length: {input.Length}");
                return false;
            }

            // 3. 模式匹配检查
            var patterns = await _context.SpamPatterns
                .Where(p => p.IsEnabled && p.PatternType == inputType)
                .Select(p => p.Pattern)
                .ToListAsync();

            foreach (var pattern in patterns)
            {
                if (Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase))
                {
                    _logger.LogWarning($"Spam pattern detected in {inputType}: {pattern}");
                    return false;
                }
            }

            // 4. 敏感词检查
            var sensitiveWords = await _context.SensitiveWords
                .Where(w => w.IsEnabled)
                .Select(w => w.Word)
                .ToListAsync();

            foreach (var word in sensitiveWords)
            {
                if (input.Contains(word, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning($"Sensitive word detected in {inputType}: {word}");
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error validating {inputType}");
            return false;
        }
    }

    public async Task<bool> ValidateIpAddressAsync(string ipAddress)
    {
        try
        {
            // 1. 检查IP是否被封禁
            if (_ipFailedAttempts.TryGetValue(ipAddress, out var failedAttempts))
            {
                if (failedAttempts >= MaxFailedAttempts)
                {
                    _logger.LogWarning($"IP {ipAddress} is blocked due to failed attempts");
                    return false;
                }
                _ipFailedAttempts[ipAddress]++;
            }
            else
            {
                _ipFailedAttempts[ipAddress] = 1;
            }

            // 2. 检查数据库中的IP黑名单
            var isBlocked = await _context.BlockedIps
                .AnyAsync(b => b.IpAddress == ipAddress && b.IsEnabled);

            if (isBlocked)
            {
                _logger.LogWarning($"IP {ipAddress} is in blacklist");
                return false;
            }

            // 3. 检查访问频率
            var recentAccessCount = await _context.AccessLogs
                .CountAsync(l => l.IpAddress == ipAddress && 
                               l.CreatedAt >= DateTime.UtcNow.AddMinutes(-1));

            if (recentAccessCount > 100) // 每分钟最多100次请求
            {
                _logger.LogWarning($"Rate limit exceeded for IP {ipAddress}");
                await BlockIpAddressAsync(ipAddress);
                return false;
            }

            // 4. 记录访问日志
            _context.AccessLogs.Add(new AccessLog
            {
                IpAddress = ipAddress,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error validating IP {ipAddress}");
            return false;
        }
    }

    public async Task<bool> ValidateFileAsync(byte[] fileContent, string fileName)
    {
        try
        {
            // 1. 文件大小检查
            if (fileContent == null || fileContent.Length == 0 || fileContent.Length > 50 * 1024 * 1024) // 50MB
            {
                _logger.LogWarning($"Invalid file size: {fileName}");
                return false;
            }

            // 2. 文件类型检查
            var fileSignature = await GetFileSignatureAsync(null);
            var isValidType = await _context.FileSignatures
                .AnyAsync(s => s.Signature == fileSignature && s.IsEnabled);

            if (!isValidType)
            {
                _logger.LogWarning($"Invalid file type: {fileName}");
                return false;
            }

            // 3. 病毒扫描
            if (!await ScanForVirusAsync(fileContent))
            {
                _logger.LogWarning($"Virus detected in file: {fileName}");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error validating file {fileName}");
            return false;
        }
    }

    public async Task<bool> CheckDatabaseConnectionAsync()
    {
        try
        {
            // 1. 检查数据库连接
            return await _context.Database.CanConnectAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database connection check failed");
            return false;
        }
    }

    public async Task<bool> CheckDataConsistencyAsync()
    {
        try
        {
            // 1. 检查用户-帖子关系
            var orphanedPosts = await _context.Posts
                .AnyAsync(p => !_context.Users.Any(u => u.Id == p.AuthorId));

            if (orphanedPosts)
            {
                _logger.LogError("Orphaned posts detected");
                return false;
            }

            // 2. 检查评论-帖子关系
            var orphanedComments = await _context.Comments
                .AnyAsync(c => !_context.Posts.Any(p => p.Id == c.PostId));

            if (orphanedComments)
            {
                _logger.LogError("Orphaned comments detected");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Data consistency check failed");
            return false;
        }
    }

    private async Task BlockIpAddressAsync(string ipAddress)
    {
        _ipFailedAttempts[ipAddress]++;
        
        if (_ipFailedAttempts[ipAddress] >= MaxFailedAttempts)
        {
            await BlockIpAsync(ipAddress, "Rate limit exceeded");
        }
    }

    private async Task<string> GetFileSignatureAsync(IFormFile? file)
    {
        if (file == null)
        {
            return string.Empty;
        }

        using var stream = file.OpenReadStream();
        var buffer = new byte[4096];
        var memory = new Memory<byte>(buffer);
        var bytesRead = await stream.ReadAsync(memory);
        return bytesRead > 0 ? BitConverter.ToString(buffer, 0, Math.Min(bytesRead, 8)).Replace("-", "") : string.Empty;
    }

    private bool AnalyzePeFile(byte[] fileContent)
    {
        try
        {
            // 1. 检查DOS头
            if (fileContent.Length < 64 || fileContent[0] != 'M' || fileContent[1] != 'Z')
            {
                return false;
            }

            // 2. 获取PE头偏移
            var peOffset = BitConverter.ToInt32(fileContent, 0x3C);
            if (peOffset < 0 || peOffset > fileContent.Length - 4)
            {
                return false;
            }

            // 3. 检查PE签名
            if (fileContent[peOffset] != 'P' || fileContent[peOffset + 1] != 'E' ||
                fileContent[peOffset + 2] != 0 || fileContent[peOffset + 3] != 0)
            {
                return false;
            }

            // 4. 检查可疑区段
            var numberOfSections = BitConverter.ToInt16(fileContent, peOffset + 6);
            var sizeOfOptionalHeader = BitConverter.ToInt16(fileContent, peOffset + 20);
            var sectionTableOffset = peOffset + 24 + sizeOfOptionalHeader;

            for (int i = 0; i < numberOfSections; i++)
            {
                var sectionOffset = sectionTableOffset + (40 * i);
                if (sectionOffset + 40 > fileContent.Length)
                {
                    return false;
                }

                // 获取区段名称
                var sectionName = Encoding.ASCII.GetString(fileContent, sectionOffset, 8).TrimEnd('\0');
                
                // 获取区段特征
                var characteristics = BitConverter.ToUInt32(fileContent, sectionOffset + 36);
                
                // 检查可疑区段特征
                if ((characteristics & 0x20000020) == 0x20000020) // 可执行且可写
                {
                    _logger.LogWarning($"Suspicious section detected: {sectionName}");
                    return false;
                }
            }

            // 5. 检查导入表
            var importTableRva = BitConverter.ToInt32(fileContent, peOffset + 24 + 104);
            if (importTableRva != 0)
            {
                var suspiciousImports = new[]
                {
                    "WriteProcessMemory",
                    "VirtualAllocEx",
                    "CreateRemoteThread",
                    "SetWindowsHookEx",
                    "GetAsyncKeyState",
                    "CreateProcess",
                    "ShellExecute"
                };

                // 简单的字符串搜索
                var fileString = Encoding.ASCII.GetString(fileContent);
                foreach (var import in suspiciousImports)
                {
                    if (fileString.Contains(import))
                    {
                        _logger.LogWarning($"Suspicious import detected: {import}");
                        return false;
                    }
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing PE file");
            return false;
        }
    }

    private async Task<bool> ScanForVirusAsync(byte[] fileContent)
    {
        try
        {
            // 1. 基础文件检查
            if (fileContent == null || fileContent.Length == 0)
            {
                _logger.LogWarning("Empty file detected");
                return false;
            }

            // 2. 文件大小检查
            if (fileContent.Length < 10 || fileContent.Length > 500 * 1024 * 1024) // 10B - 500MB
            {
                _logger.LogWarning($"Suspicious file size: {fileContent.Length} bytes");
                return false;
            }

            // 3. 检查可执行文件
            if (IsPeFile(fileContent))
            {
                if (!AnalyzePeFile(fileContent))
                {
                    _logger.LogWarning("Suspicious PE file detected");
                    return false;
                }
            }

            // 4. 检查恶意脚本特征
            var scriptPatterns = new[]
            {
                @"powershell -e",           // PowerShell encoded command
                @"cmd\.exe /c",             // CMD command
                @"wget http",               // Download command
                @"curl http",               // Download command
                @"Invoke-Expression",       // PowerShell IEX
                @"iex\(",                   // PowerShell IEX shorthand
                @"rundll32\.exe",           // RunDLL32
                @"regsvr32\.exe",          // RegSvr32
                @"wscript\.exe",           // Windows Script Host
                @"cscript\.exe",           // Windows Script Host
                @"mshta\.exe",             // HTML Application Host
                @"certutil -urlcache",      // CertUtil download
                @"bitsadmin /transfer",     // BITS transfer
                @"net user",                // User management
                @"net localgroup",          // Group management
                @"reg add",                 // Registry modification
                @"sc create",               // Service creation
                @"schtasks /create",        // Scheduled task creation
                @"attrib \+h",             // File attribute modification
                @"vssadmin delete",         // Volume shadow copy deletion
                @"bcdedit /set",           // Boot configuration modification
                @"wmic process call create" // WMIC process creation
            };

            var fileText = Encoding.UTF8.GetString(fileContent);
            foreach (var pattern in scriptPatterns)
            {
                if (Regex.IsMatch(fileText, pattern, RegexOptions.IgnoreCase))
                {
                    _logger.LogWarning($"Malicious script pattern detected: {pattern}");
                    return false;
                }
            }

            // 5. 检查文件熵值（检测加密/混淆代码）
            var entropy = CalculateEntropy(fileContent);
            if (entropy > 7.5)
            {
                _logger.LogWarning($"High entropy detected: {entropy}");
                return false;
            }

            // 6. 检查已知的恶意软件特征
            var malwareSignatures = await _context.FileSignatures
                .Where(s => !s.IsEnabled && s.IsBlocked)
                .Select(s => s.Signature)
                .ToListAsync();

            var fileSignature = await GetFileSignatureAsync(null);
            if (malwareSignatures.Contains(fileSignature))
            {
                _logger.LogWarning($"Known malware signature detected: {fileSignature}");
                return false;
            }

            // 7. 检查异常字节序列
            if (ContainsAnomalousBytes(fileContent))
            {
                _logger.LogWarning("Anomalous byte sequence detected");
                return false;
            }

            // 8. 检查文件结构完整性
            if (!ValidateFileStructure(fileContent))
            {
                _logger.LogWarning("Invalid file structure detected");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during virus scan");
            return false; // 出错时默认阻止
        }
    }

    private bool IsPeFile(byte[] fileContent)
    {
        if (fileContent.Length < 64)
            return false;

        // 检查DOS头
        if (fileContent[0] != 'M' || fileContent[1] != 'Z')
            return false;

        // 获取PE头偏移
        var peOffset = BitConverter.ToInt32(fileContent, 0x3C);
        if (peOffset < 0 || peOffset > fileContent.Length - 4)
            return false;

        // 检查PE签名
        return fileContent[peOffset] == 'P' && 
               fileContent[peOffset + 1] == 'E' &&
               fileContent[peOffset + 2] == 0 && 
               fileContent[peOffset + 3] == 0;
    }

    private bool ContainsAnomalousBytes(byte[] data)
    {
        try
        {
            // 1. 检查空字节序列
            int consecutiveZeros = 0;
            foreach (var b in data)
            {
                if (b == 0)
                {
                    consecutiveZeros++;
                    if (consecutiveZeros > 1000) // 超过1000个连续空字节
                    {
                        return true;
                    }
                }
                else
                {
                    consecutiveZeros = 0;
                }
            }

            // 2. 检查重复字节序列
            var patterns = new Dictionary<byte[], int>();
            const int patternLength = 16;
            for (int i = 0; i <= data.Length - patternLength; i++)
            {
                var pattern = new byte[patternLength];
                Array.Copy(data, i, pattern, 0, patternLength);
                
                if (!patterns.ContainsKey(pattern))
                {
                    patterns[pattern] = 1;
                }
                else
                {
                    patterns[pattern]++;
                    if (patterns[pattern] > 100) // 同一模式重复超过100次
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for anomalous bytes");
            return true; // 出错时默认阻止
        }
    }

    private bool ValidateFileStructure(byte[] data)
    {
        try
        {
            // 1. 检查文件头
            if (data.Length < 4)
            {
                return false;
            }

            // 2. 检查常见文件格式的结构
            var fileType = DetermineFileType(data);
            switch (fileType)
            {
                case "PDF":
                    return ValidatePdfStructure(data);
                case "ZIP":
                    return ValidateZipStructure(data);
                case "PNG":
                    return ValidatePngStructure(data);
                case "JPEG":
                    return ValidateJpegStructure(data);
                default:
                    return false; // 未知格式默认通过
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating file structure");
            return false;
        }
    }

    private string DetermineFileType(byte[] data)
    {
        if (data.Length < 4) return "Unknown";

        if (data[0] == 0x25 && data[1] == 0x50 && data[2] == 0x44 && data[3] == 0x46)
            return "PDF";
        if (data[0] == 0x50 && data[1] == 0x4B && data[2] == 0x03 && data[3] == 0x04)
            return "ZIP";
        if (data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47)
            return "PNG";
        if (data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF)
            return "JPEG";

        return "Unknown";
    }

    private bool ValidatePdfStructure(byte[] data)
    {
        try
        {
            // 1. 检查PDF头部
            if (!data.Take(4).SequenceEqual(new byte[] { 0x25, 0x50, 0x44, 0x46 }))
                return false;

            // 2. 检查PDF版本
            var versionBytes = data.Skip(5).Take(3).ToArray();
            var version = Encoding.ASCII.GetString(versionBytes);
            if (!Regex.IsMatch(version, @"1\.[0-7]"))
                return false;

            // 3. 检查文件尾部
            var tailBytes = data.Skip(data.Length - 6).Take(6).ToArray();
            var tail = Encoding.ASCII.GetString(tailBytes);
            return tail.Contains("%%EOF");
        }
        catch
        {
            return false;
        }
    }

    private bool ValidateZipStructure(byte[] data)
    {
        try
        {
            // 1. 检查ZIP头部
            if (!data.Take(4).SequenceEqual(new byte[] { 0x50, 0x4B, 0x03, 0x04 }))
                return false;

            // 2. 检查中央目录结构
            var endOfCentralDir = new byte[] { 0x50, 0x4B, 0x05, 0x06 };
            return data.Length > 22 && // 最小ZIP文件大小
                   FindSequence(data, endOfCentralDir) != -1;
        }
        catch
        {
            return false;
        }
    }

    private bool ValidatePngStructure(byte[] data)
    {
        try
        {
            // 1. 检查PNG头部
            if (!data.Take(8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
                return false;

            // 2. 检查IHDR块
            var ihdr = new byte[] { 0x49, 0x48, 0x44, 0x52 };
            return FindSequence(data, ihdr) == 8; // IHDR必须是第一个块
        }
        catch
        {
            return false;
        }
    }

    private bool ValidateJpegStructure(byte[] data)
    {
        try
        {
            // 1. 检查JPEG头部
            if (data[0] != 0xFF || data[1] != 0xD8)
                return false;

            // 2. 检查JPEG尾部
            return data[data.Length - 2] == 0xFF && 
                   data[data.Length - 1] == 0xD9;
        }
        catch
        {
            return false;
        }
    }

    private int FindSequence(byte[] data, byte[] sequence)
    {
        for (int i = 0; i <= data.Length - sequence.Length; i++)
        {
            bool found = true;
            for (int j = 0; j < sequence.Length; j++)
            {
                if (data[i + j] != sequence[j])
                {
                    found = false;
                    break;
                }
            }
            if (found) return i;
        }
        return -1;
    }

    private double CalculateEntropy(byte[] data)
    {
        var frequencies = new int[256];
        foreach (var b in data)
        {
            frequencies[b]++;
        }

        double entropy = 0;
        var length = data.Length;
        foreach (var frequency in frequencies)
        {
            if (frequency == 0) continue;
            var probability = (double)frequency / length;
            entropy -= probability * Math.Log2(probability);
        }

        return entropy;
    }
} 