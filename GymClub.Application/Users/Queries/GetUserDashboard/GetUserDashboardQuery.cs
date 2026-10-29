using GymClub.Application.Common.Interfaces;
using GymClub.Application.Orders.Queries.GetUserOrders;
using GymClub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Users.Queries.GetUserDashboard;

public class UserDashboardDto
{
    public int TotalOrders { get; set; }
    public int PendingOrders { get; set; }
    public int ShippedOrders { get; set; }
    public int AddressCount { get; set; }
    public int CartItems { get; set; }
    public List<OrderListItemDto> RecentOrders { get; set; } = new();
}

public record GetUserDashboardQuery : IRequest<UserDashboardDto>;

public class GetUserDashboardQueryHandler : IRequestHandler<GetUserDashboardQuery, UserDashboardDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _current;

    public GetUserDashboardQueryHandler(IApplicationDbContext db, ICurrentUserService current)
    {
        _db = db; _current = current;
    }

    public async Task<UserDashboardDto> Handle(GetUserDashboardQuery req, CancellationToken ct)
    {
        if (_current.UserId is null) return new UserDashboardDto();
        var userId = _current.UserId.Value;

        var orders = await _db.Orders.AsNoTracking()
            .Where(o => o.UserId == userId)
            .ToListAsync(ct);

        var cartItems = await _db.CartItems
            .Where(i => i.Cart.UserId == userId)
            .SumAsync(i => (int?)i.Quantity, ct) ?? 0;

        var addresses = await _db.Addresses.CountAsync(a => a.UserId == userId, ct);

        return new UserDashboardDto
        {
            TotalOrders = orders.Count,
            PendingOrders = orders.Count(o => o.Status == OrderStatus.Paid || o.Status == OrderStatus.Confirmed),
            ShippedOrders = orders.Count(o => o.Status == OrderStatus.Shipped),
            AddressCount = addresses,
            CartItems = cartItems,
            RecentOrders = orders
                .OrderByDescending(o => o.CreatedAt)
                .Take(5)
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
                }).ToList()
        };
    }
}