using System;
using System.Collections.Generic;

namespace ZhihuClone.Core.Interfaces
{
    public interface ISearchHighlightService : IDisposable
    {
        string HighlightText(string text, string searchQuery, string highlightTag = "em", int fragmentSize = 150);
        string HighlightTitle(string title, string searchQuery, string highlightTag = "em");
        IEnumerable<string> ExtractKeywords(string text, int maxKeywords = 5);
    }
} 