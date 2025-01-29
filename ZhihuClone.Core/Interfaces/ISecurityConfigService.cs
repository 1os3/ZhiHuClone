using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZhihuClone.Core.Models.Security;

namespace ZhihuClone.Core.Interfaces
{
    public interface ISecurityConfigService
    {
        // 基础配置操作
        Task<SecurityConfig> CreateConfigAsync(SecurityConfig config);
        Task<SecurityConfig?> GetConfigByKeyAsync(string key);
        Task<bool> UpdateConfigAsync(SecurityConfig config);
        Task<bool> DeleteConfigAsync(string key);
        Task<IEnumerable<SecurityConfig>> GetAllConfigsAsync(bool includeDeprecated = false);
        
        // 配置管理
        Task<IEnumerable<SecurityConfig>> GetConfigsByCategoryAsync(string category);
        Task<bool> EnableConfigAsync(string key);
        Task<bool> DisableConfigAsync(string key);
        Task<bool> ValidateConfigValueAsync(string key, string value);
        Task<bool> ResetToDefaultAsync(string key);
        
        // 配置版本控制
        Task<SecurityConfig?> GetConfigVersionAsync(string key, int version);
        Task<IEnumerable<SecurityConfig>> GetConfigHistoryAsync(string key);
        Task<bool> DeprecateConfigAsync(string key, string reason);
        Task<bool> RestoreConfigVersionAsync(string key, int version);
        
        // 配置依赖管理
        Task<IEnumerable<SecurityConfig>> GetDependentConfigsAsync(string key);
        Task<IEnumerable<SecurityConfig>> GetAffectedConfigsAsync(string key);
        Task<bool> ValidateDependenciesAsync(string key);
        
        // 配置导入导出
        Task<string> ExportConfigsAsync(string? category = null);
        Task<bool> ImportConfigsAsync(string configJson, bool overwrite = false);
        
        // 系统配置
        Task<bool> RequireSystemRestartAsync();
        Task<IEnumerable<string>> GetModifiedSystemConfigsAsync();
        Task<Dictionary<string, string>> GetEncryptedConfigsAsync();
    }
} 