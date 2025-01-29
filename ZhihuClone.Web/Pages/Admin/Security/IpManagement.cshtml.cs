using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ZhihuClone.Core.Interfaces.Security;
using ZhihuClone.Core.Models.Security;
using ZhihuClone.Web.Models.Admin;

namespace ZhihuClone.Web.Pages.Admin.Security
{
    public class IpManagementSearchParameters
    {
        public string? IpAddress { get; set; }
        public string? Status { get; set; }
        public string? Reason { get; set; }
    }

    [Authorize(Roles = "Admin")]
    public class IpManagementModel : PageModel
    {
        private readonly IBlockedIpRepository _blockedIpRepository;

        public IpManagementModel(IBlockedIpRepository blockedIpRepository)
        {
            _blockedIpRepository = blockedIpRepository;
        }

        public List<BlockedIp> BlockedIps { get; set; } = new();
        public IpManagementSearchParameters SearchParams { get; set; } = new();
        public int CurrentPage { get; set; }
        public int PageSize { get; set; } = 10;
        public int TotalItems { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalItems / (double)PageSize);

        public async Task OnGetAsync(
            string? ipAddress = null,
            string? status = null,
            string? reason = null,
            int page = 1)
        {
            CurrentPage = page < 1 ? 1 : page;
            SearchParams = new IpManagementSearchParameters
            {
                IpAddress = ipAddress,
                Status = status,
                Reason = reason
            };

            // 获取总记录数
            TotalItems = await _blockedIpRepository.CountAsync(
                ipAddress: SearchParams.IpAddress,
                status: SearchParams.Status,
                reason: SearchParams.Reason);

            // 获取分页数据
            BlockedIps = await _blockedIpRepository.GetPagedAsync(
                page: CurrentPage,
                pageSize: PageSize,
                ipAddress: SearchParams.IpAddress,
                status: SearchParams.Status,
                reason: SearchParams.Reason);
        }

        public string GetPageUrl(int pageNumber)
        {
            var queryParams = new List<string>();

            if (!string.IsNullOrEmpty(SearchParams.IpAddress))
                queryParams.Add($"ipAddress={SearchParams.IpAddress}");

            if (!string.IsNullOrEmpty(SearchParams.Status))
                queryParams.Add($"status={SearchParams.Status}");

            if (!string.IsNullOrEmpty(SearchParams.Reason))
                queryParams.Add($"reason={SearchParams.Reason}");

            queryParams.Add($"page={pageNumber}");

            return $"/Admin/Security/IpManagement?{string.Join("&", queryParams)}";
        }
    }
} 