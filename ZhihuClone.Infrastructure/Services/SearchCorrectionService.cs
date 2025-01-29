using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Directory = Lucene.Net.Store.Directory;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;
using Lucene.Net.Util;
using ZhihuClone.Core.Interfaces;
using ZhihuClone.Core.Models;
using Microsoft.EntityFrameworkCore;
using ZhihuClone.Infrastructure.Data;
using ZhihuClone.Core.Models.Content;
using ZhihuClone.Core.Models.Search;

namespace ZhihuClone.Infrastructure.Services
{
    public class SearchCorrectionService : ISearchCorrectionService
    {
        private readonly LuceneVersion _version = LuceneVersion.LUCENE_48;
        private readonly Analyzer _analyzer;
        private readonly Directory _directory;
        private readonly ICacheService _cacheService;
        private readonly IPostRepository _postRepository;
        private readonly ITopicRepository _topicRepository;
        private readonly ApplicationDbContext _context;
        private readonly ISearchHistoryService _searchHistoryService;
        private static readonly TimeSpan CacheExpiry = TimeSpan.FromHours(24);

        public SearchCorrectionService(ICacheService cacheService, 
            IPostRepository postRepository,
            ITopicRepository topicRepository,
            ApplicationDbContext context,
            ISearchHistoryService searchHistoryService)
        {
            _cacheService = cacheService;
            _postRepository = postRepository;
            _topicRepository = topicRepository;
            _context = context;
            _searchHistoryService = searchHistoryService;
            _analyzer = new StandardAnalyzer(_version);
            _directory = new RAMDirectory();
        }

        public async Task<List<SearchSuggestion>> GetSuggestionsAsync(string searchTerm)
        {
            var suggestions = new List<SearchSuggestion>();

            // 搜索帖子
            var posts = await _postRepository.SearchAsync(searchTerm, 1, 5);
            foreach (var post in posts)
            {
                suggestions.Add(new SearchSuggestion
                {
                    Id = post.Id,
                    Type = SearchSuggestionType.Post,
                    Title = post.Title,
                    Content = post.Content
                });
            }

            // 搜索话题
            var topics = await _topicRepository.SearchAsync(searchTerm, 1, 5);
            foreach (var topic in topics)
            {
                suggestions.Add(new SearchSuggestion
                {
                    Id = topic.Id,
                    Type = SearchSuggestionType.Topic,
                    Title = topic.Name,
                    Content = topic.Description
                });
            }

            return suggestions;
        }

        public async Task<string> SuggestCorrectionAsync(string query)
        {
            if (string.IsNullOrEmpty(query))
                return query;

            var cacheKey = $"search_correction:{query}";
            var cachedCorrection = await _cacheService.GetAsync<string>(cacheKey);
            if (!string.IsNullOrEmpty(cachedCorrection))
                return cachedCorrection;

            try
            {
                await BuildSpellCheckerIndexAsync();
                var suggestions = new List<string>();

                foreach (var term in query.Split(' '))
                {
                    if (term.Length <= 2) // 忽略过短的词
                    {
                        suggestions.Add(term);
                        continue;
                    }

                    var correctedTerm = await GetCorrectionForTermAsync(term);
                    suggestions.Add(correctedTerm ?? term);
                }

                var correctedQuery = string.Join(" ", suggestions);
                if (correctedQuery != query)
                {
                    await _cacheService.SetAsync(cacheKey, correctedQuery, CacheExpiry);
                    return correctedQuery;
                }

                return query;
            }
            catch (Exception)
            {
                return query;
            }
        }

        private async Task BuildSpellCheckerIndexAsync()
        {
            var cacheKey = "spellchecker_index_built";
            if (await _cacheService.ExistsAsync(cacheKey))
                return;

            using var writer = new IndexWriter(_directory, 
                new IndexWriterConfig(_version, _analyzer));

            // 从帖子标题和内容中收集词汇
            var posts = await _postRepository.GetAllAsync();
            foreach (var post in posts)
            {
                if (!string.IsNullOrEmpty(post.Title))
                    AddTermsToIndex(writer, post.Title);
                if (!string.IsNullOrEmpty(post.Content))
                    AddTermsToIndex(writer, post.Content);
            }

            // 从话题名称中收集词汇
            var topics = await _topicRepository.GetAllAsync();
            foreach (var topic in topics)
            {
                if (!string.IsNullOrEmpty(topic.Name))
                    AddTermsToIndex(writer, topic.Name);
            }

            writer.Commit();
            await _cacheService.SetAsync(cacheKey, true, CacheExpiry);
        }

        private void AddTermsToIndex(IndexWriter writer, string text)
        {
            using var tokenStream = _analyzer.GetTokenStream("content", new System.IO.StringReader(text));
            var termAttribute = tokenStream.AddAttribute<Lucene.Net.Analysis.TokenAttributes.ICharTermAttribute>();

            tokenStream.Reset();
            while (tokenStream.IncrementToken())
            {
                var term = termAttribute.ToString();
                if (term.Length > 2) // 忽略过短的词
                {
                    var doc = new Lucene.Net.Documents.Document();
                    doc.Add(new Lucene.Net.Documents.TextField("term", term,
                        Lucene.Net.Documents.Field.Store.YES));
                    writer.AddDocument(doc);
                }
            }
            tokenStream.End();
        }

        private async Task<string?> GetCorrectionForTermAsync(string term)
        {
            return await Task.Run(() =>
            {
                using var reader = DirectoryReader.Open(_directory);
                var searcher = new IndexSearcher(reader);

                var query = new FuzzyQuery(new Term("term", term.ToLowerInvariant()), 2);
                var topDocs = searcher.Search(query, 1);

                if (topDocs.TotalHits > 0)
                {
                    var doc = searcher.Doc(topDocs.ScoreDocs[0].Doc);
                    var suggestion = doc.Get("term");
                    if (suggestion != term)
                        return suggestion;
                }

                return null;
            }).ConfigureAwait(false);
        }

        public void Dispose()
        {
            _analyzer?.Dispose();
            _directory?.Dispose();
        }

        public async Task<string> GetCorrectedQueryAsync(string query)
        {
            if (string.IsNullOrEmpty(query))
                return query;

            // 1. 检查是否是常见错误
            var correctedQuery = await CheckCommonErrorsAsync(query);
            if (correctedQuery != query)
                return correctedQuery;

            // 2. 检查同义词
            correctedQuery = await CheckSynonymsAsync(query);
            if (correctedQuery != query)
                return correctedQuery;

            // 3. 检查热门搜索
            var hotSearches = await _searchHistoryService.GetTrendingSearchesAsync();
            foreach (var hotSearch in hotSearches)
            {
                if (IsCloseMatch(query, hotSearch))
                    return hotSearch;
            }

            return query;
        }

        private async Task<string> CheckCommonErrorsAsync(string query)
        {
            // 从数据库中获取常见错误模式
            var patterns = await _context.SpamPatterns
                .Where(p => p.IsEnabled && !p.IsBlocked)
                .ToListAsync();

            foreach (var pattern in patterns)
            {
                if (query.Contains(pattern.Pattern, StringComparison.OrdinalIgnoreCase))
                {
                    return query.Replace(pattern.Pattern, pattern.Replacement ?? pattern.Pattern, StringComparison.OrdinalIgnoreCase);
                }
            }

            return query;
        }

        private async Task<string> CheckSynonymsAsync(string query)
        {
            // 从数据库中获取同义词
            var synonyms = await _context.Synonyms
                .Where(s => s.Word == query)
                .Select(s => s.SynonymWord)
                .ToListAsync();

            if (synonyms.Any())
            {
                // 返回搜索次数最多的同义词
                var mostSearchedSynonym = await Task.WhenAll(synonyms.Select(async s =>
                {
                    var searchCount = await GetSearchCountAsync(s);
                    return new { Synonym = s, Count = searchCount };
                }));

                var bestMatch = mostSearchedSynonym.OrderByDescending(x => x.Count).First();
                return bestMatch.Synonym;
            }

            return query;
        }

        private async Task<int> GetSearchCountAsync(string keyword)
        {
            return await _context.SearchHistories
                .Where(sh => sh.Keyword == keyword)
                .SumAsync(sh => sh.SearchCount);
        }

        private bool IsCloseMatch(string query1, string query2)
        {
            if (string.IsNullOrEmpty(query1) || string.IsNullOrEmpty(query2))
                return false;

            // 使用Levenshtein距离算法计算相似度
            var distance = ComputeLevenshteinDistance(query1.ToLower(), query2.ToLower());
            var maxLength = Math.Max(query1.Length, query2.Length);
            var similarity = 1 - ((double)distance / maxLength);

            return similarity >= 0.8; // 相似度阈值
        }

        private int ComputeLevenshteinDistance(string s, string t)
        {
            var n = s.Length;
            var m = t.Length;
            var d = new int[n + 1, m + 1];

            if (n == 0) return m;
            if (m == 0) return n;

            for (var i = 0; i <= n; i++) d[i, 0] = i;
            for (var j = 0; j <= m; j++) d[0, j] = j;

            for (var i = 1; i <= n; i++)
            {
                for (var j = 1; j <= m; j++)
                {
                    var cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            return d[n, m];
        }
    }
} 