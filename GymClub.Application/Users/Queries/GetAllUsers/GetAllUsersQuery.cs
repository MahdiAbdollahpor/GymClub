using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using GymClub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Users.Queries.GetAllUsers;

public class UserListItemDto
{
    public Guid Id { get; set; }
    public int RowNumber { get; set; }              // ← جدید
    public string PhoneNumber { get; set; } = string.Empty;
    public string DisplayUserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsPhoneConfirmed { get; set; }
    public bool IsActive { get; set; }              // ← جدید
    public DateTime CreatedAt { get; set; }
    public int OrderCount { get; set; }
    public int WorkoutRequestCount { get; set; }    // ← جدید
    public int ProgramCount { get; set; }           // ← جدید
}

public record GetAllUsersQuery(string? Search = null, int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<UserListItemDto>>;

public class GetAllUsersQueryHandler
    : IRequestHandler<GetAllUsersQuery, PagedResult<UserListItemDto>>
{
    private readonly IApplicationDbContext _db;
    public GetAllUsersQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<UserListItemDto>> Handle(GetAllUsersQuery req, CancellationToken ct)
    {
        var q = _db.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(req.Search))
            q = q.Where(u => u.PhoneNumber!.Contains(req.Search)
                          || u.DisplayUserName.Contains(req.Search)
                          || u.FullName.Contains(req.Search));

        var total = await q.CountAsync(ct);

        var raw = await q
            .OrderByDescending(u => u.CreatedAt)
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .Select(u => new
            {
                u.Id,
                u.PhoneNumber,
                u.DisplayUserName,
                u.FullName,
                u.Role,
                u.IsPhoneConfirmed,
                u.IsActive,
                u.CreatedAt,
                OrderCount = _db.Orders.Count(o => o.UserId == u.Id),
                WorkoutRequestCount = _db.WorkoutRequests.Count(w => w.UserId == u.Id),
                ProgramCount = _db.WorkoutPrograms.Count(p => p.UserId == u.Id)
            })
            .ToListAsync(ct);

        var startIndex = (req.Page - 1) * req.PageSize;
        var items = raw.Select((u, i) => new UserListItemDto
        {
            Id = u.Id,
            RowNumber = startIndex + i + 1,
            PhoneNumber = u.PhoneNumber ?? "",
            DisplayUserName = u.DisplayUserName,
            FullName = u.FullName,
            Role = u.Role,
            IsPhoneConfirmed = u.IsPhoneConfirmed,
            IsActive = u.IsActive,
            CreatedAt = u.CreatedAt,
            OrderCount = u.OrderCount,
            WorkoutRequestCount = u.WorkoutRequestCount,
            ProgramCount = u.ProgramCount
        }).ToList();

        return new PagedResult<UserListItemDto>
        {
            Items = items,
            Page = req.Page,
            PageSize = req.PageSize,
            TotalCount = total
        };
    }
}