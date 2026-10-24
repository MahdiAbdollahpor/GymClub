using GymClub.Domain.Common;

namespace GymClub.Domain.Entities;

public class Product : SoftDeletableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public int Stock { get; set; }
    public bool IsActive { get; set; } = true;

    public ProductCategory Category { get; set; } = null!;
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public ICollection<ProductAttribute> Attributes { get; set; } = new List<ProductAttribute>();
}