namespace GymClub.Application.Products.Dtos;

public class ProductDetailsDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public string CategoryTitle { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public int Stock { get; set; }

    public bool IsActive { get; set; }             
    public DateTime CreatedAt { get; set; }         
    public DateTime? UpdatedAt { get; set; }


    public List<ProductImageDto> Images { get; set; } = new();
    public List<ProductAttributeDto> Attributes { get; set; } = new();
    public decimal FinalPrice => DiscountPrice ?? Price;
    public bool HasDiscount => DiscountPrice.HasValue && DiscountPrice < Price;
}

public class ProductImageDto
{
    public Guid Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsMain { get; set; }
}

public class ProductAttributeDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}