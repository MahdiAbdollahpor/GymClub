using GymClub.Domain.Enums;

namespace GymClub.Application.Articles.Dtos;

public class ArticleCardDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public ArticleType Type { get; set; }
    public string? CategoryTitle { get; set; }
    public DateTime? PublishedAt { get; set; }
    public int ViewCount { get; set; }
    public bool HasVideo { get; set; }
}