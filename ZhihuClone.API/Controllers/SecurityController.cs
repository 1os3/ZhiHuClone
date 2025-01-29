using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ZhihuClone.Core.Interfaces.Security;
using ZhihuClone.Core.Models.Security;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using ZhihuClone.Core.Interfaces;

namespace ZhihuClone.API.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class SecurityController : ControllerBase
    {
        private readonly IBlockedIpRepository _blockedIpRepository;
        private readonly ISensitiveWordRepository _sensitiveWordRepository;
        private readonly ISpamPatternRepository _spamPatternRepository;
        private readonly IFileSignatureRepository _fileSignatureRepository;
        private readonly IFirewallService _firewallService;
        private readonly IUserService _userService;

        public SecurityController(
            IBlockedIpRepository blockedIpRepository,
            ISensitiveWordRepository sensitiveWordRepository,
            ISpamPatternRepository spamPatternRepository,
            IFileSignatureRepository fileSignatureRepository,
            IFirewallService firewallService,
            IUserService userService)
        {
            _blockedIpRepository = blockedIpRepository;
            _sensitiveWordRepository = sensitiveWordRepository;
            _spamPatternRepository = spamPatternRepository;
            _fileSignatureRepository = fileSignatureRepository;
            _firewallService = firewallService;
            _userService = userService;
        }

        #region IP管理
        [HttpGet("blocked-ips")]
        public async Task<IActionResult> GetBlockedIps(
            [FromQuery] string? ipAddress = null,
            [FromQuery] string? status = null,
            [FromQuery] string? reason = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var totalCount = await _blockedIpRepository.CountAsync(ipAddress, status, reason);
                var blockedIps = await _blockedIpRepository.GetPagedAsync(page, pageSize, ipAddress, status, reason);

                return Ok(new
                {
                    TotalCount = totalCount,
                    Items = blockedIps
                });
            }
            catch (Exception)
            {
                return StatusCode(500, "获取封禁IP列表时发生错误");
            }
        }

        [HttpGet("blocked-ips/{id}")]
        public async Task<IActionResult> GetBlockedIp(int id)
        {
            try
            {
                var blockedIp = await _blockedIpRepository.GetByIdAsync(id);
                if (blockedIp == null)
                    return NotFound("未找到指定的封禁记录");

                return Ok(blockedIp);
            }
            catch (Exception)
            {
                return StatusCode(500, "获取封禁IP详情时发生错误");
            }
        }

        [HttpPost("blocked-ips")]
        public async Task<IActionResult> BlockIp([FromBody] BlockIpRequest request)
        {
            try
            {
                var blockedIp = new BlockedIp
                {
                    IpAddress = request.IpAddress,
                    Reason = request.Reason,
                    BlockType = request.BlockType,
                    IsEnabled = true,
                    CreatedAt = DateTime.UtcNow
                };

                await _blockedIpRepository.AddAsync(blockedIp);
                return Ok(blockedIp);
            }
            catch (Exception)
            {
                return StatusCode(500, "添加封禁IP时发生错误");
            }
        }

        [HttpPut("blocked-ips/{id}")]
        public async Task<IActionResult> UpdateBlockedIp(int id, [FromBody] UpdateBlockedIpRequest request)
        {
            try
            {
                var blockedIp = await _blockedIpRepository.GetByIdAsync(id);
                if (blockedIp == null)
                    return NotFound("未找到指定的封禁记录");

                blockedIp.Reason = request.Reason;
                blockedIp.IsEnabled = request.IsEnabled;
                blockedIp.UpdatedAt = DateTime.UtcNow;

                await _blockedIpRepository.UpdateAsync(blockedIp);
                return Ok(blockedIp);
            }
            catch (Exception)
            {
                return StatusCode(500, "更新封禁IP时发生错误");
            }
        }

        [HttpDelete("blocked-ips/{id}")]
        public async Task<IActionResult> DeleteBlockedIp(int id)
        {
            try
            {
                await _blockedIpRepository.DeleteAsync(id);
                return Ok();
            }
            catch (Exception)
            {
                return StatusCode(500, "删除封禁IP时发生错误");
            }
        }

        [HttpGet("blocked-ips/{ipAddress}/history")]
        public async Task<IActionResult> GetBlockHistory(string ipAddress, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var history = await _blockedIpRepository.GetHistoryAsync(ipAddress, page, pageSize);
                return Ok(history);
            }
            catch (Exception)
            {
                return StatusCode(500, "获取IP封禁历史时发生错误");
            }
        }
        #endregion

        #region 敏感词管理
        [HttpPost("sensitivewords")]
        public async Task<IActionResult> AddSensitiveWord([FromBody] AddSensitiveWordRequest request)
        {
            try
            {
                var word = new SensitiveWord
                {
                    Word = request.Word,
                    Category = request.Category,
                    Level = request.Level,
                    IsRegex = request.IsRegex,
                    IsEnabled = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedByUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0")
                };

                await _sensitiveWordRepository.AddAsync(word);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("sensitivewords/{id}")]
        public async Task<IActionResult> GetSensitiveWord(int id)
        {
            try
            {
                var word = await _sensitiveWordRepository.GetByIdAsync(id);
                return Ok(word);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("sensitivewords/{id}")]
        public async Task<IActionResult> UpdateSensitiveWord(int id, [FromBody] UpdateSensitiveWordRequest request)
        {
            try
            {
                var word = await _sensitiveWordRepository.GetByIdAsync(id);
                if (word == null)
                    return NotFound();

                word.Word = request.Word;
                word.Category = request.Category;
                word.Level = request.Level;
                word.IsRegex = request.IsRegex;
                word.UpdatedAt = DateTime.UtcNow;

                await _sensitiveWordRepository.UpdateAsync(word);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("sensitivewords/{id}/{action}")]
        public async Task<IActionResult> ToggleSensitiveWord(int id, string action)
        {
            try
            {
                var word = await _sensitiveWordRepository.GetByIdAsync(id);
                if (word == null)
                    return NotFound();

                word.IsEnabled = action == "enable";
                word.UpdatedAt = DateTime.UtcNow;

                await _sensitiveWordRepository.UpdateAsync(word);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("sensitivewords/{id}")]
        public async Task<IActionResult> DeleteSensitiveWord(int id)
        {
            try
            {
                await _sensitiveWordRepository.DeleteAsync(id);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("sensitivewords/import")]
        public async Task<IActionResult> ImportSensitiveWords(IFormFile file, string category, int level)
        {
            try
            {
                var words = new List<SensitiveWord>();
                using (var reader = new System.IO.StreamReader(file.OpenReadStream()))
                {
                    string? line;
                    while ((line = await reader.ReadLineAsync()) is not null)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            words.Add(new SensitiveWord
                            {
                                Word = line.Trim(),
                                Category = category,
                                Level = level,
                                IsRegex = false,
                                IsEnabled = true,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow,
                                CreatedByUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0")
                            });
                        }
                    }
                }

                await _sensitiveWordRepository.BulkInsertAsync(words);
                return Ok(new { success = true, count = words.Count });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
        #endregion

        #region 垃圾信息规则管理
        [HttpPost("spampatterns")]
        public async Task<IActionResult> AddSpamPattern([FromBody] AddSpamPatternRequest request)
        {
            try
            {
                var pattern = new SpamPattern
                {
                    Pattern = request.Pattern,
                    Category = request.Category,
                    IsRegex = request.IsRegex,
                    IsEnabled = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedByUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0")
                };

                await _spamPatternRepository.AddAsync(pattern);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("spampatterns/{id}")]
        public async Task<IActionResult> GetSpamPattern(int id)
        {
            try
            {
                var pattern = await _spamPatternRepository.GetByIdAsync(id);
                return Ok(pattern);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("spampatterns/{id}")]
        public async Task<IActionResult> UpdateSpamPattern(int id, [FromBody] UpdateSpamPatternRequest request)
        {
            try
            {
                var pattern = await _spamPatternRepository.GetByIdAsync(id);
                if (pattern == null)
                    return NotFound();

                pattern.Pattern = request.Pattern;
                pattern.Category = request.Category;
                pattern.IsRegex = request.IsRegex;
                pattern.UpdatedAt = DateTime.UtcNow;

                await _spamPatternRepository.UpdateAsync(pattern);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("spampatterns/{id}/{action}")]
        public async Task<IActionResult> ToggleSpamPattern(int id, string action)
        {
            try
            {
                var pattern = await _spamPatternRepository.GetByIdAsync(id);
                if (pattern == null)
                    return NotFound();

                pattern.IsEnabled = action == "enable";
                pattern.UpdatedAt = DateTime.UtcNow;

                await _spamPatternRepository.UpdateAsync(pattern);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("spampatterns/{id}")]
        public async Task<IActionResult> DeleteSpamPattern(int id)
        {
            try
            {
                await _spamPatternRepository.DeleteAsync(id);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("spampatterns/import")]
        public async Task<IActionResult> ImportSpamPatterns(IFormFile file, string category)
        {
            try
            {
                var patterns = new List<SpamPattern>();
                using (var reader = new System.IO.StreamReader(file.OpenReadStream()))
                {
                    string? line;
                    while ((line = await reader.ReadLineAsync()) is not null)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            patterns.Add(new SpamPattern
                            {
                                Pattern = line.Trim(),
                                Category = category,
                                IsRegex = false,
                                IsEnabled = true,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow,
                                CreatedByUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0")
                            });
                        }
                    }
                }

                await _spamPatternRepository.BulkInsertAsync(patterns);
                return Ok(new { success = true, count = patterns.Count });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
        #endregion

        #region 文件签名管理
        [HttpPost("filesignatures")]
        public async Task<IActionResult> AddFileSignature([FromBody] AddFileSignatureRequest request)
        {
            try
            {
                var signature = new FileSignature
                {
                    Signature = request.Signature,
                    FileType = request.FileType,
                    IsWhitelisted = request.IsWhitelisted,
                    Description = request.Description,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedByUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0")
                };

                await _fileSignatureRepository.AddAsync(signature);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("filesignatures/{id}")]
        public async Task<IActionResult> GetFileSignature(int id)
        {
            try
            {
                var signature = await _fileSignatureRepository.GetByIdAsync(id);
                return Ok(signature);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("filesignatures/{id}")]
        public async Task<IActionResult> UpdateFileSignature(int id, [FromBody] UpdateFileSignatureRequest request)
        {
            try
            {
                var signature = await _fileSignatureRepository.GetByIdAsync(id);
                if (signature == null)
                    return NotFound();

                signature.Signature = request.Signature;
                signature.FileType = request.FileType;
                signature.IsWhitelisted = request.IsWhitelisted;
                signature.Description = request.Description;
                signature.UpdatedAt = DateTime.UtcNow;

                await _fileSignatureRepository.UpdateAsync(signature);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("filesignatures/{id}/{action}")]
        public async Task<IActionResult> ToggleFileSignature(int id, string action)
        {
            try
            {
                var signature = await _fileSignatureRepository.GetByIdAsync(id);
                if (signature == null)
                    return NotFound();

                signature.IsWhitelisted = action == "whitelist";
                signature.UpdatedAt = DateTime.UtcNow;

                await _fileSignatureRepository.UpdateAsync(signature);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("filesignatures/{id}")]
        public async Task<IActionResult> DeleteFileSignature(int id)
        {
            try
            {
                await _fileSignatureRepository.DeleteAsync(id);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("filesignatures/import")]
        public async Task<IActionResult> ImportFileSignatures(IFormFile file, string fileType, bool isWhitelisted)
        {
            try
            {
                var signatures = new List<FileSignature>();
                using (var reader = new System.IO.StreamReader(file.OpenReadStream()))
                {
                    string? line;
                    while ((line = await reader.ReadLineAsync()) is not null)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            signatures.Add(new FileSignature
                            {
                                Signature = line.Trim(),
                                FileType = fileType,
                                IsWhitelisted = isWhitelisted,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow,
                                CreatedByUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0")
                            });
                        }
                    }
                }

                await _fileSignatureRepository.BulkInsertAsync(signatures);
                return Ok(new { success = true, count = signatures.Count });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
        #endregion

        [HttpGet("blocked-ips/history")]
        public async Task<IActionResult> GetBlockedIpsHistory([FromQuery] string? ipAddress, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var nameIdClaim = User.FindFirst("nameid")?.Value ?? throw new InvalidOperationException("用户ID不存在");
            var userId = int.Parse(nameIdClaim);
            var user = await _userService.GetByIdAsync(userId);

            if (user == null)
                return NotFound("用户不存在");

            var blockedIps = await _blockedIpRepository.GetAllAsync(ipAddress);
            return Ok(blockedIps);
        }

        [HttpPost("sensitive-words/bulk")]
        public async Task<IActionResult> BulkAddSensitiveWords([FromBody] List<SensitiveWord> words)
        {
            var nameIdClaim = User.FindFirst("nameid")?.Value ?? throw new InvalidOperationException("用户ID不存在");
            var userId = int.Parse(nameIdClaim);
            var user = await _userService.GetByIdAsync(userId);

            if (user == null)
                return NotFound("用户不存在");

            foreach (var word in words)
            {
                word.CreatedByUserId = user.Id;
                word.UpdatedByUserId = user.Id;
                await _sensitiveWordRepository.AddAsync(word);
            }

            return Ok();
        }

        [HttpPost("spam-patterns/bulk")]
        public async Task<IActionResult> BulkAddSpamPatterns([FromBody] List<SpamPattern> patterns)
        {
            var nameIdClaim = User.FindFirst("nameid")?.Value ?? throw new InvalidOperationException("用户ID不存在");
            var userId = int.Parse(nameIdClaim);
            var user = await _userService.GetByIdAsync(userId);

            if (user == null)
                return NotFound("用户不存在");

            foreach (var pattern in patterns)
            {
                pattern.CreatedByUserId = user.Id;
                pattern.UpdatedByUserId = user.Id;
                await _spamPatternRepository.AddAsync(pattern);
            }

            return Ok();
        }

        [HttpGet("file-signatures")]
        public async Task<IActionResult> GetFileSignatures([FromQuery] string? signature, [FromQuery] string? fileType, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var fileSignatures = await _fileSignatureRepository.GetAllAsync(page, pageSize);
            return Ok(fileSignatures);
        }

        [HttpPost("file-signatures")]
        public async Task<IActionResult> AddFileSignature([FromBody] FileSignature fileSignature)
        {
            var nameIdClaim = User.FindFirst("nameid")?.Value ?? throw new InvalidOperationException("用户ID不存在");
            var userId = int.Parse(nameIdClaim);
            var user = await _userService.GetByIdAsync(userId);

            if (user == null)
                return NotFound("用户不存在");

            fileSignature.CreatedByUserId = user.Id;
            fileSignature.UpdatedByUserId = user.Id;
            fileSignature.IsEnabled = true;

            await _fileSignatureRepository.AddAsync(fileSignature);
            return Ok(fileSignature);
        }

        [HttpPut("file-signatures/{id}")]
        public async Task<IActionResult> UpdateFileSignature(int id, [FromBody] FileSignature fileSignature)
        {
            var nameIdClaim = User.FindFirst("nameid")?.Value ?? throw new InvalidOperationException("用户ID不存在");
            var userId = int.Parse(nameIdClaim);
            var user = await _userService.GetByIdAsync(userId);

            if (user == null)
                return NotFound("用户不存在");

            var existingSignature = await _fileSignatureRepository.GetByIdAsync(id);
            if (existingSignature == null)
            {
                return NotFound();
            }

            existingSignature.UpdatedByUserId = user.Id;
            existingSignature.UpdatedAt = DateTime.UtcNow;
            existingSignature.IsEnabled = fileSignature.IsEnabled;
            existingSignature.Description = fileSignature.Description;

            await _fileSignatureRepository.UpdateAsync(existingSignature);
            return Ok(existingSignature);
        }

        [HttpDelete("file-signatures/{id}/delete")]
        public async Task<IActionResult> DeleteFileSignatureById(int id)
        {
            var existingSignature = await _fileSignatureRepository.GetByIdAsync(id);
            if (existingSignature == null)
            {
                return NotFound();
            }

            await _fileSignatureRepository.DeleteAsync(id);
            return Ok();
        }
    }

    #region Request Models
    public class BlockIpRequest
    {
        [Required]
        public string IpAddress { get; set; } = string.Empty;
        [Required]
        public string Reason { get; set; } = string.Empty;
        public string BlockType { get; set; } = "Manual";
    }

    public class UpdateBlockedIpRequest
    {
        [Required]
        public string Reason { get; set; } = string.Empty;
        [Required]
        public bool IsEnabled { get; set; }
    }

    public class AddSensitiveWordRequest
    {
        [Required]
        public string Word { get; set; } = string.Empty;
        [Required]
        public string Category { get; set; } = string.Empty;
        public int Level { get; set; }
        public bool IsRegex { get; set; }
    }

    public class UpdateSensitiveWordRequest
    {
        [Required]
        public string Word { get; set; } = string.Empty;
        [Required]
        public string Category { get; set; } = string.Empty;
        public int Level { get; set; }
        public bool IsRegex { get; set; }
    }

    public class AddSpamPatternRequest
    {
        [Required]
        public string Pattern { get; set; } = string.Empty;
        [Required]
        public string Category { get; set; } = string.Empty;
        public bool IsRegex { get; set; }
    }

    public class UpdateSpamPatternRequest
    {
        [Required]
        public string Pattern { get; set; } = string.Empty;
        [Required]
        public string Category { get; set; } = string.Empty;
        public bool IsRegex { get; set; }
    }

    public class AddFileSignatureRequest
    {
        [Required]
        public string Signature { get; set; } = string.Empty;
        [Required]
        public string FileType { get; set; } = string.Empty;
        public bool IsWhitelisted { get; set; }
        public string? Description { get; set; }
    }

    public class UpdateFileSignatureRequest
    {
        [Required]
        public string Signature { get; set; } = string.Empty;
        [Required]
        public string FileType { get; set; } = string.Empty;
        public bool IsWhitelisted { get; set; }
        public string? Description { get; set; }
    }
    #endregion
} 