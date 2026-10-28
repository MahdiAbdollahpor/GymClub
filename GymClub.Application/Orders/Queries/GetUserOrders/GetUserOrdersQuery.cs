using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using GymClub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Orders.Queries.GetUserOrders;

public class OrderListItemDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public decimal Total { get; set; }
    public int ItemCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? TrackingCode { get; set; }
    public string? CancelReason { get; set; }
}

public record GetUserOrdersQuery : IRequest<List<OrderListItemDto>>;

public class GetUserOrdersQueryHandler : IRequestHandler<GetUserOrdersQuery, List<OrderListItemDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _current;

    public GetUserOrdersQueryHandler(IApplicationDbContext db, ICurrentUserService current)
    {
        _db = db; _current = current;
    }

    public async Task<List<OrderListItemDto>> Handle(GetUserOrdersQuery req, CancellationToken ct)
    {
        if (_current.UserId is null) return new();

        return await _db.Orders
            .AsNoTracking()
            .Where(o => o.UserId == _current.UserId.Value)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new OrderListItemDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                Status = o.Status,
                Total = o.Total,
                ItemCount = o.Items.Count,
                CreatedAt = o.CreatedAt,
                PaidAt = o.PaidAt,
                TrackingCode = o.TrackingCode,
                CancelReason = o.CancelReason
            })
            .ToListAsync(ct);
    }
}