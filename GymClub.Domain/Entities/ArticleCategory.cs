using GymClub.Domain.Common;

namespace GymClub.Domain.Entities;

public class ArticleCategory : SoftDeletableEntity
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public int SortOrder { get; set; }

    public ArticleCategory? Parent { get; set; }
    public ICollection<ArticleCategory> Children { get; set; } = new List<ArticleCategory>();
    public ICollection<Article> Articles { get; set; } = new List<Article>();
}