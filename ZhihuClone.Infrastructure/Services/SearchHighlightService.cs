using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search.Highlight;
using Lucene.Net.Util;
using ZhihuClone.Core.Interfaces;

namespace ZhihuClone.Infrastructure.Services
{
    public class SearchHighlightService : ISearchHighlightService
    {
        private readonly LuceneVersion _version = LuceneVersion.LUCENE_48;
        private readonly Analyzer _analyzer;
        private readonly QueryParser _queryParser;
        private const string DefaultHighlightTag = "em";
        private const int DefaultFragmentSize = 150;
        private const string DefaultEllipsis = "...";

        public SearchHighlightService()
        {
            _analyzer = new StandardAnalyzer(_version);
            _queryParser = new QueryParser(_version, "content", _analyzer);
        }

        public string HighlightText(string text, string searchQuery, string highlightTag = DefaultHighlightTag, 
            int fragmentSize = DefaultFragmentSize)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(searchQuery))
                return text;

            try
            {
                var query = _queryParser.Parse(searchQuery);
                var scorer = new QueryScorer(query);
                var formatter = new SimpleHTMLFormatter($"<{highlightTag}>", $"</{highlightTag}>");
                var highlighter = new Highlighter(formatter, scorer)
                {
                    TextFragmenter = new SimpleFragmenter(fragmentSize)
                };

                var tokenStream = _analyzer.GetTokenStream("content", new System.IO.StringReader(text));
                var result = highlighter.GetBestFragments(tokenStream, text, 3, DefaultEllipsis);

                return string.IsNullOrEmpty(result) ? text : result;
            }
            catch (Exception)
            {
                return text;
            }
        }

        public string HighlightTitle(string title, string searchQuery, string highlightTag = DefaultHighlightTag)
        {
            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(searchQuery))
                return title;

            try
            {
                var query = _queryParser.Parse(searchQuery);
                var scorer = new QueryScorer(query);
                var formatter = new SimpleHTMLFormatter($"<{highlightTag}>", $"</{highlightTag}>");
                var highlighter = new Highlighter(formatter, scorer);

                var tokenStream = _analyzer.GetTokenStream("title", new System.IO.StringReader(title));
                var result = highlighter.GetBestFragment(tokenStream, title);

                return string.IsNullOrEmpty(result) ? title : result;
            }
            catch (Exception)
            {
                return title;
            }
        }

        public IEnumerable<string> ExtractKeywords(string text, int maxKeywords = 5)
        {
            if (string.IsNullOrEmpty(text))
                return Enumerable.Empty<string>();

            try
            {
                var tokenStream = _analyzer.GetTokenStream("content", new System.IO.StringReader(text));
                var termAttribute = tokenStream.AddAttribute<Lucene.Net.Analysis.TokenAttributes.ICharTermAttribute>();
                
                var keywords = new HashSet<string>();
                tokenStream.Reset();

                while (tokenStream.IncrementToken() && keywords.Count < maxKeywords)
                {
                    var term = termAttribute.ToString();
                    if (term.Length > 1) // 忽略单字符词
                    {
                        keywords.Add(term);
                    }
                }

                tokenStream.End();
                tokenStream.Dispose();

                return keywords;
            }
            catch (Exception)
            {
                return Enumerable.Empty<string>();
            }
        }

        public void Dispose()
        {
            _analyzer?.Dispose();
        }

        public string HighlightTitle(string title, string query)
        {
            throw new NotImplementedException();
        }

        public string HighlightText(string text, string query)
        {
            throw new NotImplementedException();
        }
    }
} 