using GymClub.Application.Common.Interfaces;
using GymClub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Dashboard.Queries.GetAdminDashboardStats;

public class AdminDashboardStatsDto
{
    // فروش
    public decimal TotalRevenue { get; set; }              // مجموع سفارش‌های Paid/Confirmed/Shipped
    public decimal TodayRevenue { get; set; }
    public decimal MonthRevenue { get; set; }
    public int TotalOrders { get; set; }
    public int PendingOrders { get; set; }                 // Paid (منتظر تأیید)
    public int ConfirmedOrders { get; set; }
    public int ShippedOrders { get; set; }
    public int CancelledOrders { get; set; }

    // کاربران
    public int TotalUsers { get; set; }
    public int TodayUsers { get; set; }
    public int MonthUsers { get; set; }

    // محتوا
    public int TotalArticles { get; set; }
    public int PublishedArticles { get; set; }
    public int DraftArticles { get; set; }

    // محصولات
    public int TotalProducts { get; set; }
    public int ActiveProducts { get; set; }
    public int OutOfStockProducts { get; set; }
    public int LowStockProducts { get; set; }              // Stock <= 5

    // برنامه ورزشی
    public int TotalWorkoutRequests { get; set; }
    public int PendingWorkoutRequests { get; set; }        // AnswersSubmitted
    public int CompletedWorkoutRequests { get; set; }

    // لیست‌های اخیر
    public List<RecentOrderDto> RecentOrders { get; set; } = new();
    public List<RecentWorkoutRequestDto> RecentWorkoutRequests { get; set; } = new();
    public List<LowStockProductDto> LowStockProductsList { get; set; } = new();

    // نمودار ۳۰ روز اخیر
    public List<DailySalesDto> Last30DaysSales { get; set; } = new();
}

public class RecentOrderDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string ReceiverName { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class RecentWorkoutRequestDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserPhone { get; set; } = string.Empty;
    public WorkoutRequestStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class LowStockProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Stock { get; set; }
}

public class DailySalesDto
{
    public string Date { get; set; } = string.Empty;       // yyyy/MM/dd
    public decimal Amount { get; set; }
    public int Count { get; set; }
}

public record GetAdminDashboardStatsQuery : IRequest<AdminDashboardStatsDto>;

public class GetAdminDashboardStatsQueryHandler
    : IRequestHandler<GetAdminDashboardStatsQuery, AdminDashboardStatsDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public GetAdminDashboardStatsQueryHandler(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));

        if (_db.Orders is null) throw new InvalidOperationException("DI: Orders is null");
        if (_db.Users is null) throw new InvalidOperationException("DI: Users is null");
        if (_db.Articles is null) throw new InvalidOperationException("DI: Articles is null");
        if (_db.Products is null) throw new InvalidOperationException("DI: Products is null");
        if (_db.WorkoutRequests is null) throw new InvalidOperationException("DI: WorkoutRequests is null");
    }

    public async Task<AdminDashboardStatsDto> Handle(GetAdminDashboardStatsQuery req, CancellationToken ct)
    {
        var now = _clock.UtcNow;
        var todayStart = now.Date;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var paidStatuses = new[] { OrderStatus.Paid, OrderStatus.Confirmed, OrderStatus.Shipped };

        var ordersQuery = _db.Orders.AsNoTracking();

        var dto = new AdminDashboardStatsDto
        {
            TotalRevenue = await ordersQuery
                .Where(o => paidStatuses.Contains(o.Status))
                .SumAsync(o => (decimal?)o.Total, ct) ?? 0,
            TodayRevenue = await ordersQuery
                .Where(o => paidStatuses.Contains(o.Status) && o.PaidAt >= todayStart)
                .SumAsync(o => (decimal?)o.Total, ct) ?? 0,
            MonthRevenue = await ordersQuery
                .Where(o => paidStatuses.Contains(o.Status) && o.PaidAt >= monthStart)
                .SumAsync(o => (decimal?)o.Total, ct) ?? 0,

            TotalOrders = await ordersQuery.CountAsync(ct),
            PendingOrders = await ordersQuery.CountAsync(o => o.Status == OrderStatus.Paid, ct),
            ConfirmedOrders = await ordersQuery.CountAsync(o => o.Status == OrderStatus.Confirmed, ct),
            ShippedOrders = await ordersQuery.CountAsync(o => o.Status == OrderStatus.Shipped, ct),
            CancelledOrders = await ordersQuery.CountAsync(o => o.Status == OrderStatus.Cancelled, ct),

            TotalUsers = await _db.Users.CountAsync(ct),
            TodayUsers = await _db.Users.CountAsync(u => u.CreatedAt >= todayStart, ct),
            MonthUsers = await _db.Users.CountAsync(u => u.CreatedAt >= monthStart, ct),

            TotalArticles = await _db.Articles.CountAsync(a => !a.IsDeleted, ct),
            PublishedArticles = await _db.Articles.CountAsync(a => !a.IsDeleted && a.Status == ArticleStatus.Published, ct),
            DraftArticles = await _db.Articles.CountAsync(a => !a.IsDeleted && a.Status == ArticleStatus.Draft, ct),

            TotalProducts = await _db.Products.CountAsync(p => !p.IsDeleted, ct),
            ActiveProducts = await _db.Products.CountAsync(p => !p.IsDeleted && p.IsActive, ct),
            OutOfStockProducts = await _db.Products.CountAsync(p => !p.IsDeleted && p.Stock <= 0, ct),
            LowStockProducts = await _db.Products.CountAsync(p => !p.IsDeleted && p.Stock > 0 && p.Stock <= 5, ct),

            TotalWorkoutRequests = await _db.WorkoutRequests.CountAsync(ct),
            PendingWorkoutRequests = await _db.WorkoutRequests.CountAsync(w => w.Status == WorkoutRequestStatus.AnswersSubmitted, ct),
            CompletedWorkoutRequests = await _db.WorkoutRequests.CountAsync(w => w.Status == WorkoutRequestStatus.Completed, ct),
        };

        // سفارش‌های اخیر
        dto.RecentOrders = await ordersQuery
            .OrderByDescending(o => o.CreatedAt)
            .Take(8)
            .Select(o => new RecentOrderDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                ReceiverName = o.ReceiverName,
                Total = o.Total,
                Status = o.Status,
                CreatedAt = o.CreatedAt
            })
            .ToListAsync(ct);

        // درخواست‌های برنامه اخیر
        dto.RecentWorkoutRequests = await _db.WorkoutRequests
            .AsNoTracking()
            .OrderByDescending(w => w.CreatedAt)
            .Take(5)
            .Select(w => new RecentWorkoutRequestDto
            {
                Id = w.Id,
                UserName = w.User.DisplayUserName,
                UserPhone = w.User.PhoneNumber ?? "",
                Status = w.Status,
                CreatedAt = w.CreatedAt
            })
            .ToListAsync(ct);

        // محصولات کم‌موجود
        dto.LowStockProductsList = await _db.Products
            .AsNoTracking()
            .Where(p => !p.IsDeleted && p.Stock >= 0 && p.Stock <= 5)
            .OrderBy(p => p.Stock)
            .Take(5)
            .Select(p => new LowStockProductDto { Id = p.Id, Name = p.Name, Stock = p.Stock })
            .ToListAsync(ct);

        // نمودار ۳۰ روز اخیر
        var from = now.Date.AddDays(-29);
        var salesByDay = await ordersQuery
            .Where(o => paidStatuses.Contains(o.Status) && o.PaidAt >= from)
            .GroupBy(o => o.PaidAt!.Value.Date)
            .Select(g => new { Date = g.Key, Amount = g.Sum(x => x.Total), Count = g.Count() })
            .ToListAsync(ct);

        var days = Enumerable.Range(0, 30)
            .Select(i => from.AddDays(i))
            .ToList();

        dto.Last30DaysSales = days.Select(d =>
        {
            var m = salesByDay.FirstOrDefault(x => x.Date == d);
            return new DailySalesDto
            {
                Date = d.ToString("yyyy/MM/dd"),
                Amount = m?.Amount ?? 0,
                Count = m?.Count ?? 0
            };
        }).ToList();

        return dto;
    }
}