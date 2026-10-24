namespace GymClub.Application.Products.Dtos;

public class ProductCategoryDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public int ProductCount { get; set; }
}