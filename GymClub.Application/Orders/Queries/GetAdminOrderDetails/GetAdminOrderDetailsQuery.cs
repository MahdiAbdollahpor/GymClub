using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using GymClub.Application.Orders.Queries.GetOrderDetails;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Orders.Queries.GetAdminOrderDetails;

public record GetAdminOrderDetailsQuery(Guid OrderId) : IRequest<Result<OrderDetailsDto>>;

public class GetAdminOrderDetailsQueryHandler
    : IRequestHandler<GetAdminOrderDetailsQuery, Result<OrderDetailsDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IImageStorageService _images;

    public GetAdminOrderDetailsQueryHandler(IApplicationDbContext db, IImageStorageService images)
    {
        _db = db; _images = images;
    }

    public async Task<Result<OrderDetailsDto>> Handle(GetAdminOrderDetailsQuery req, CancellationToken ct)
    {
        var o = await _db.Orders.AsNoTracking()
            .Include(x => x.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(x => x.Id == req.OrderId, ct);
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