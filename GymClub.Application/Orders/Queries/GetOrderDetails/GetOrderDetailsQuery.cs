using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using GymClub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Orders.Queries.GetOrderDetails;

public class OrderDetailsDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal Total { get; set; }
    public string ReceiverName { get; set; } = string.Empty;
    public string ReceiverPhone { get; set; } = string.Empty;
    public string FullAddress { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? TrackingCode { get; set; }
    public string? CancelReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? ShippedAt { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
}

public class OrderItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductImageUrl { get; set; }
    public string? ProductSlug { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }
}

public record GetOrderDetailsQuery(Guid OrderId) : IRequest<Result<OrderDetailsDto>>;

public class GetOrderDetailsQueryHandler
    : IRequestHandler<GetOrderDetailsQuery, Result<OrderDetailsDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _current;
    private readonly IImageStorageService _images;

    public GetOrderDetailsQueryHandler(IApplicationDbContext db, ICurrentUserService current, IImageStorageService images)
    {
        _db = db; _current = current; _images = images;
    }

    public async Task<Result<OrderDetailsDto>> Handle(GetOrderDetailsQuery req, CancellationToken ct)
    {
        if (_current.UserId is null) return Result<OrderDetailsDto>.Fail("ابتدا وارد شوید");

        var o = await _db.Orders
            .AsNoTracking()
            .Include(x => x.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(x => x.Id == req.OrderId && x.UserId == _current.UserId.Value, ct);
        if (o is null) return Result<OrderDetailsDto>.Fail("سفارش یافت نشد");

        return Result<OrderDetailsDto>.Ok(new OrderDetailsDto
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            Status = o.Status,
            SubTotal = o.SubTotal,
            DiscountAmount = o.DiscountAmount,
            ShippingCost = o.ShippingCost,
            Total = o.Total,
            ReceiverName = o.ReceiverName,
            ReceiverPhone = o.ReceiverPhone,
            FullAddress = o.FullAddress,
            Province = o.Province,
            City = o.City,
            TrackingCode = o.TrackingCode,
            CancelReason = o.CancelReason,
            CreatedAt = o.CreatedAt,
            PaidAt = o.PaidAt,
            ShippedAt = o.ShippedAt,
            Items = o.Items.Select(i => new OrderItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.ProductNameSnapshot,
                ProductImageUrl = string.IsNullOrEmpty(i.ProductImageSnapshot) ? null : _images.GetPublicUrl(i.ProductImageSnapshot),
                ProductSlug = i.Product?.Slug,
                UnitPrice = i.UnitPriceSnapshot,
                Quantity = i.Quantity,
                LineTotal = i.LineTotal
            }).ToList()
        });
    }
}