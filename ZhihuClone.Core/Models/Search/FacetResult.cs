using System.Collections.Generic;

namespace ZhihuClone.Core.Models.Search
{
    public class FacetResult
    {
        public string Name { get; set; } = string.Empty;
        public List<FacetValue> Values { get; set; } = new List<FacetValue>();
    }

    public class FacetValue
    {
        public string Value { get; set; } = string.Empty;
        public int Count { get; set; }
    }
} 