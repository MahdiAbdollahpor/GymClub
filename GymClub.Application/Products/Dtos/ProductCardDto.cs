namespace GymClub.Application.Products.Dtos;

public class ProductCardDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? MainImageUrl { get; set; }
    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public int Stock { get; set; }
    public string CategoryTitle { get; set; } = string.Empty;
    public decimal FinalPrice => DiscountPrice ?? Price;
    public bool HasDiscount => DiscountPrice.HasValue && DiscountPrice < Price;
}