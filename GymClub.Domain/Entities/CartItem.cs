using GymClub.Domain.Common;

namespace GymClub.Domain.Entities;

public class CartItem : BaseEntity
{
    public Guid CartId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPriceSnapshot { get; set; }

    public Cart Cart { get; set; } = null!;
    public Product Product { get; set; } = null!;
}