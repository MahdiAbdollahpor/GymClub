using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using GymClub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Orders.Queries.GetAdminOrders;

public record GetAdminOrdersQuery(string? Search = null, OrderStatus? Status = null, int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<OrderAdminListItemDto>>;

public class OrderAdminListItemDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string UserPhone { get; set; } = string.Empty;
    public string ReceiverName { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public decimal Total { get; set; }
    public int ItemCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? TrackingCode { get; set; }
}

public class GetAdminOrdersQueryHandler
    : IRequestHandler<GetAdminOrdersQuery, PagedResult<OrderAdminListItemDto>>
{
    private readonly IApplicationDbContext _db;
    public GetAdminOrdersQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<OrderAdminListItemDto>> Handle(GetAdminOrdersQuery req, CancellationToken ct)
    {
        var q = _db.Orders.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(req.Search))
            q = q.Where(o => o.OrderNumber.Contains(req.Search) || o.ReceiverName.Contains(req.Search) || o.ReceiverPhone.Contains(req.Search));
        if (req.Status.HasValue) q = q.Where(o => o.Status == req.Status);

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(o => o.CreatedAt)
            .Skip((req.Page - 1) * req.PageSize).Take(req.PageSize)
            .Select(o => new OrderAdminListItemDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                UserPhone = o.User.PhoneNumber ?? "",
                ReceiverName = o.ReceiverName,
                Status = o.Status,
                Total = o.Total,
                ItemCount = o.Items.Count,
                CreatedAt = o.CreatedAt,
                TrackingCode = o.TrackingCode
            }).ToListAsync(ct);

        return new PagedResult<OrderAdminListItemDto>
        {
            Items = items,
            Page = req.Page,
            PageSize = req.PageSize,
            TotalCount = total
        };
    }
}