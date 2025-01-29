using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ZhihuClone.Core.Interfaces.Security;
using ZhihuClone.Core.Models.Security;

namespace ZhihuClone.Web.Pages.Admin.Security
{
    [Authorize(Roles = "Admin")]
    public class FileSignaturesModel : PageModel
    {
        private readonly IFileSignatureRepository _fileSignatureRepository;

        public FileSignaturesModel(IFileSignatureRepository fileSignatureRepository)
        {
            _fileSignatureRepository = fileSignatureRepository;
            FileSignatures = new List<FileSignature>();
            FileTypes = new List<string>();
            SearchParams = new SearchParameters();
        }

        public List<FileSignature> FileSignatures { get; set; }
        public List<string> FileTypes { get; set; }
        public SearchParameters SearchParams { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; } = 10;
        public int TotalItems { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalItems / (double)PageSize);

        public async Task OnGetAsync(
            string? signature = null,
            string? fileType = null,
            string? status = null,
            int page = 1)
        {
            CurrentPage = page < 1 ? 1 : page;
            SearchParams = new SearchParameters
            {
                Signature = signature,
                FileType = fileType,
                Status = status
            };

            // 获取所有文件类型
            FileTypes = await _fileSignatureRepository.GetFileTypesAsync();

            // 获取总记录数
            TotalItems = await _fileSignatureRepository.CountAsync(
                signature: SearchParams.Signature,
                fileType: SearchParams.FileType,
                isWhitelisted: GetIsWhitelisted(SearchParams.Status));

            // 获取分页数据
            FileSignatures = await _fileSignatureRepository.GetPagedAsync(
                page: CurrentPage,
                pageSize: PageSize,
                signature: SearchParams.Signature,
                fileType: SearchParams.FileType,
                isWhitelisted: GetIsWhitelisted(SearchParams.Status));
        }

        private bool? GetIsWhitelisted(string? status)
        {
            return status switch
            {
                "whitelisted" => true,
                "blacklisted" => false,
                _ => null
            };
        }

        public string GetPageUrl(int pageNumber)
        {
            var queryParams = new List<string>();

            if (!string.IsNullOrEmpty(SearchParams.Signature))
                queryParams.Add($"signature={SearchParams.Signature}");

            if (!string.IsNullOrEmpty(SearchParams.FileType))
                queryParams.Add($"fileType={SearchParams.FileType}");

            if (!string.IsNullOrEmpty(SearchParams.Status))
                queryParams.Add($"status={SearchParams.Status}");

            queryParams.Add($"page={pageNumber}");

            return $"/Admin/Security/FileSignatures?{string.Join("&", queryParams)}";
        }
    }

    public class SearchParameters
    {
        public string? Signature { get; set; }
        public string? FileType { get; set; }
        public string? Status { get; set; }
    }
} 