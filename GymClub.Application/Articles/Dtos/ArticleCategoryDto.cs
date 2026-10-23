namespace GymClub.Application.Articles.Dtos;

public class ArticleCategoryDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public int SortOrder { get; set; }
    public int ArticleCount { get; set; }
}