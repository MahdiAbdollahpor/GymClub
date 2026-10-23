using GymClub.Domain.Common;
using GymClub.Domain.Enums;

namespace GymClub.Domain.Entities;

public class Article : SoftDeletableEntity
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string ContentHtml { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public string? VideoUrl { get; set; }
    public ArticleType Type { get; set; }
    public ArticleStatus Status { get; set; } = ArticleStatus.Draft;
    public Guid? CategoryId { get; set; }
    public Guid AuthorId { get; set; }
    public DateTime? PublishedAt { get; set; }
    public int ViewCount { get; set; }

    public ArticleCategory? Category { get; set; }
    public User Author { get; set; } = null!;
}