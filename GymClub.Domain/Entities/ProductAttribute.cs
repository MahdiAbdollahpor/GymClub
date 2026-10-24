using GymClub.Domain.Common;

namespace GymClub.Domain.Entities;

public class ProductAttribute : BaseEntity
{
    public Guid ProductId { get; set; }
    public string Title { get; set; } = string.Empty;   // "وزن"
    public string Value { get; set; } = string.Empty;   // "2 کیلوگرم"
    public int SortOrder { get; set; }

    public Product Product { get; set; } = null!;
}