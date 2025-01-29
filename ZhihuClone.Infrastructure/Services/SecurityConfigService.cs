using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZhihuClone.Core.Interfaces;
using ZhihuClone.Core.Models.Security;
using ZhihuClone.Infrastructure.Data;

namespace ZhihuClone.Infrastructure.Services
{
    public class SecurityConfigService : ISecurityConfigService
    {
        private readonly ApplicationDbContext _context;

        public SecurityConfigService(ApplicationDbContext context)
        {
            _context = context;
        }

        // 基础配置操作
        public async Task<SecurityConfig> CreateConfigAsync(SecurityConfig config)
        {
            config.CreatedAt = DateTime.UtcNow;
            config.Version = 1;
            _context.SecurityConfigs.Add(config);
            await _context.SaveChangesAsync();
            return config;
        }

        public async Task<SecurityConfig?> GetConfigByKeyAsync(string key)
        {
            return await _context.SecurityConfigs
                .Where(c => c.Key == key && !c.IsDeprecated)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> UpdateConfigAsync(SecurityConfig config)
        {
            var existingConfig = await _context.SecurityConfigs.FindAsync(config.Id);
            if (existingConfig == null) return false;

            // 创建新版本
            config.Version = existingConfig.Version + 1;
            config.UpdatedAt = DateTime.UtcNow;
            
            _context.Entry(existingConfig).CurrentValues.SetValues(config);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteConfigAsync(string key)
        {
            var config = await GetConfigByKeyAsync(key);
            if (config == null) return false;

            config.IsDeprecated = true;
            config.DeprecatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<SecurityConfig>> GetAllConfigsAsync(bool includeDeprecated = false)
        {
            var query = _context.SecurityConfigs.AsQueryable();
            if (!includeDeprecated)
                query = query.Where(c => !c.IsDeprecated);
            
            return await query.OrderBy(c => c.Order).ToListAsync();
        }

        // 配置管理
        public async Task<IEnumerable<SecurityConfig>> GetConfigsByCategoryAsync(string category)
        {
            return await _context.SecurityConfigs
                .Where(c => c.Category == category && !c.IsDeprecated)
                .OrderBy(c => c.Order)
                .ToListAsync();
        }

        public async Task<bool> EnableConfigAsync(string key)
        {
            var config = await GetConfigByKeyAsync(key);
            if (config == null) return false;

            config.IsEnabled = true;
            config.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DisableConfigAsync(string key)
        {
            var config = await GetConfigByKeyAsync(key);
            if (config == null) return false;

            config.IsEnabled = false;
            config.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ValidateConfigValueAsync(string key, string value)
        {
            var config = await GetConfigByKeyAsync(key);
            if (config == null || string.IsNullOrEmpty(config.ValidationRule)) 
                return true;

            try
            {
                // 根据数据类型进行验证
                switch (config.DataType?.ToLower())
                {
                    case "int":
                        return int.TryParse(value, out _);
                    case "bool":
                        return bool.TryParse(value, out _);
                    case "json":
                        JsonSerializer.Deserialize<JsonDocument>(value);
                        return true;
                    default:
                        // 使用正则表达式验证
                        return System.Text.RegularExpressions.Regex.IsMatch(value, config.ValidationRule);
                }
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ResetToDefaultAsync(string key)
        {
            var config = await GetConfigByKeyAsync(key);
            if (config == null || string.IsNullOrEmpty(config.DefaultValue)) 
                return false;

            config.Value = config.DefaultValue;
            config.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        // 配置版本控制
        public async Task<SecurityConfig?> GetConfigVersionAsync(string key, int version)
        {
            return await _context.SecurityConfigs
                .Where(c => c.Key == key && c.Version == version)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<SecurityConfig>> GetConfigHistoryAsync(string key)
        {
            return await _context.SecurityConfigs
                .Where(c => c.Key == key)
                .OrderByDescending(c => c.Version)
                .ToListAsync();
        }

        public async Task<bool> DeprecateConfigAsync(string key, string reason)
        {
            var config = await GetConfigByKeyAsync(key);
            if (config == null) return false;

            config.IsDeprecated = true;
            config.DeprecatedAt = DateTime.UtcNow;
            config.DeprecationReason = reason;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RestoreConfigVersionAsync(string key, int version)
        {
            var oldConfig = await GetConfigVersionAsync(key, version);
            if (oldConfig == null) return false;

            var currentConfig = await GetConfigByKeyAsync(key);
            if (currentConfig == null) return false;

            currentConfig.Value = oldConfig.Value;
            currentConfig.Version++;
            currentConfig.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        // 配置依赖管理
        public async Task<IEnumerable<SecurityConfig>> GetDependentConfigsAsync(string key)
        {
            var configs = await _context.SecurityConfigs
                .Where(c => !c.IsDeprecated && c.DependsOn != null)
                .ToListAsync();

            return configs.Where(c => 
            {
                try
                {
                    var dependencies = JsonSerializer.Deserialize<string[]>(c.DependsOn ?? "[]");
                    return dependencies?.Contains(key) ?? false;
                }
                catch
                {
                    return false;
                }
            });
        }

        public async Task<IEnumerable<SecurityConfig>> GetAffectedConfigsAsync(string key)
        {
            var config = await GetConfigByKeyAsync(key);
            if (config?.AffectedModules == null) 
                return new List<SecurityConfig>();

            try
            {
                var modules = JsonSerializer.Deserialize<string[]>(config.AffectedModules);
                if (modules == null) return new List<SecurityConfig>();

                return await _context.SecurityConfigs
                    .Where(c => !c.IsDeprecated && modules.Contains(c.Category))
                    .ToListAsync();
            }
            catch
            {
                return new List<SecurityConfig>();
            }
        }

        public async Task<bool> ValidateDependenciesAsync(string key)
        {
            var config = await GetConfigByKeyAsync(key);
            if (config?.DependsOn == null) return true;

            try
            {
                var dependencies = JsonSerializer.Deserialize<string[]>(config.DependsOn);
                if (dependencies == null) return true;

                foreach (var dep in dependencies)
                {
                    var depConfig = await GetConfigByKeyAsync(dep);
                    if (depConfig == null || !depConfig.IsEnabled)
                        return false;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        // 配置导入导出
        public async Task<string> ExportConfigsAsync(string? category = null)
        {
            var query = _context.SecurityConfigs.AsQueryable();
            if (!string.IsNullOrEmpty(category))
                query = query.Where(c => c.Category == category);

            var configs = await query.ToListAsync();
            return JsonSerializer.Serialize(configs, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }

        public async Task<bool> ImportConfigsAsync(string configJson, bool overwrite = false)
        {
            try
            {
                var configs = JsonSerializer.Deserialize<List<SecurityConfig>>(configJson);
                if (configs == null) return false;

                foreach (var config in configs)
                {
                    var existingConfig = await GetConfigByKeyAsync(config.Key);
                    if (existingConfig == null)
                    {
                        await CreateConfigAsync(config);
                    }
                    else if (overwrite)
                    {
                        config.Id = existingConfig.Id;
                        await UpdateConfigAsync(config);
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        // 系统配置
        public async Task<bool> RequireSystemRestartAsync()
        {
            var modifiedSystemConfigs = await GetModifiedSystemConfigsAsync();
            return modifiedSystemConfigs.Any();
        }

        public async Task<IEnumerable<string>> GetModifiedSystemConfigsAsync()
        {
            var configs = await _context.SecurityConfigs
                .Where(c => c.IsSystem && c.RequiresRestart && c.UpdatedAt > c.CreatedAt)
                .Select(c => c.Key)
                .ToListAsync();

            return configs;
        }

        public async Task<Dictionary<string, string>> GetEncryptedConfigsAsync()
        {
            var configs = await _context.SecurityConfigs
                .Where(c => c.IsEncrypted && !c.IsDeprecated)
                .ToDictionaryAsync(c => c.Key, c => c.Value);

            return configs;
        }
    }
} 