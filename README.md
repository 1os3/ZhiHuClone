# 知乎克隆项目 (ZhiHu Clone)

## 项目简介
这是一个基于.NET Core开发的知乎风格的问答社区平台。本项目采用清晰的分层架构，提供高性能的用户交互体验和强大的后台管理功能。

## 主要功能
### 用户系统
- 用户注册与登录
- 匿名发布功能
- 个人资料管理
- 用户认证系统

### 内容管理
- 发布/编辑/删除帖子
- 多媒体内容支持（图片、视频）
- 评论系统
- 点赞功能
- 内容举报机制
- 全文搜索功能
  - 支持按标题和内容搜索
  - 搜索结果高亮显示
  - 搜索历史记录
  - 热门搜索推荐
  - 搜索过滤和排序

### 后台管理
- 用户管理
- 内容审核
- 举报处理
- 系统配置
- 数据统计
- 搜索管理
  - 搜索统计
  - 热门搜索管理
  - 搜索日志分析

### 安全特性
- 基础防火墙功能
- 内容过滤
- DDoS防护
- 敏感信息加密

## 技术架构
### 后端架构
- 框架：.NET Core 9.0
- 数据库：SQL Server
- 缓存：Redis
- 搜索引擎：全文搜索 + ElasticSearch
- 文件存储：本地存储 + 七牛云存储

### 前端技术
- 框架：React + TypeScript
- UI库：Ant Design
- 状态管理：Redux
- 构建工具：Vite

### 项目结构
- ZhihuClone.Web：Web界面层
- ZhihuClone.API：API接口层
- ZhihuClone.Core：核心业务层
- ZhihuClone.Infrastructure：基础设施层

## 性能优化
- 数据库索引优化
- 缓存策略
- 图片压缩和CDN加速
- API性能监控

## 部署说明
### 系统要求
- Windows 10/11
- .NET Core 9.0 SDK
- SQL Server 2019+
- Redis 6.0+
- Node.js 16+

### 开发环境配置
1. 克隆代码库
2. 还原NuGet包
3. 配置数据库连接
4. 运行数据库迁移
5. 启动项目

## 使用说明
详细的使用说明请参考 [使用文档](./docs/usage.md)

## 开发计划
- [x] 基础架构搭建
- [x] 用户系统开发
- [x] 内容管理系统
- [x] 搜索功能
- [ ] 多媒体处理
- [ ] 后台管理系统
- [ ] 安全系统
- [ ] 性能优化
- [ ] 部署文档 

## 部署方法
1. 以管理员运行Install.ps1
2. 运行StartUP_API.ps1
3. 运行StartUP_Web.ps1
