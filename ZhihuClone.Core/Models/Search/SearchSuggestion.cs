namespace ZhihuClone.Core.Models.Search;

public class SearchSuggestion
{
    public int Id { get; set; }
    public SearchSuggestionType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public enum SearchSuggestionType
{
    Post,
    Topic,
    User,
    Question,
    Answer
} 