using GymClub.Domain.Common;

namespace GymClub.Domain.Entities;

public class ProductImage : BaseEntity
{
    public Guid ProductId { get; set; }
    public string Url { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsMain { get; set; }

    public Product Product { get; set; } = null!;
}