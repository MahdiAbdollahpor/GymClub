using GymClub.Domain.Enums;

namespace GymClub.Application.Articles.Dtos;

public class ArticleDetailsDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string ContentHtml { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public string? VideoUrl { get; set; }
    public ArticleType Type { get; set; }
    public Guid? CategoryId { get; set; }
    public string? CategoryTitle { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public DateTime? PublishedAt { get; set; }
    public int ViewCount { get; set; }
}