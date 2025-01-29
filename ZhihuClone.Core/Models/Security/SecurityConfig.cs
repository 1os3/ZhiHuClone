using System;
using System.ComponentModel.DataAnnotations;

namespace ZhihuClone.Core.Models.Security
{
    public class SecurityConfig
    {
        public int Id { get; set; }
        
        [Required]
        public string Key { get; set; } = null!;  // 配置键
        
        [Required]
        public string Value { get; set; } = null!; // 配置值
        
        public string? Description { get; set; }
        public string? Category { get; set; }      // 配置分类
        public bool IsEnabled { get; set; }
        public int Order { get; set; }            // 配置顺序
        
        public string? DataType { get; set; }     // 数据类型：string/int/bool/json等
        public string? ValidationRule { get; set; } // 验证规则（正则表达式）
        public string? DefaultValue { get; set; }  // 默认值
        
        public bool IsSystem { get; set; }        // 是否系统配置
        public bool IsEncrypted { get; set; }     // 是否加密存储
        public bool RequiresRestart { get; set; } // 是否需要重启
        
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        
        // 用于配置版本控制
        public int Version { get; set; }
        public bool IsDeprecated { get; set; }
        public DateTime? DeprecatedAt { get; set; }
        public string? DeprecationReason { get; set; }
        
        // 用于配置依赖关系
        public string? DependsOn { get; set; }    // 依赖的其他配置（JSON格式）
        public string? AffectedModules { get; set; } // 受影响的模块（JSON格式）
    }
} 