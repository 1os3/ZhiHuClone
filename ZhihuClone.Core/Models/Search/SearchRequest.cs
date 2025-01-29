using System;
using System.Collections.Generic;

namespace ZhihuClone.Core.Models.Search
{
    public class SearchRequest
    {
        public string Query { get; set; } = "";
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public SearchSortBy SortBy { get; set; } = SearchSortBy.Relevance;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? MinLikes { get; set; }
        public int? MinComments { get; set; }
        public bool IncludeDeleted { get; set; } = false;
        public SearchType Type { get; set; } = SearchType.All;
    }

    public enum SearchSortBy
    {
        CreatedAtDesc,
        CreatedAtAsc,
        LikesDesc,
        CommentsDesc,
        ViewsDesc,
        Relevance
    }

    public enum SearchType
    {
        All,
        Posts,
        Questions,
        Answers,
        Users
    }
} 