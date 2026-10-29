using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using GymClub.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Users.Queries.GetProfile;

public class UserProfileDto
{
    public Guid Id { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string DisplayUserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int AddressCount { get; set; }
    public int OrderCount { get; set; }
}

public record GetProfileQuery : IRequest<Result<UserProfileDto>>;

public class GetProfileQueryHandler : IRequestHandler<GetProfileQuery, Result<UserProfileDto>>
{
    private readonly UserManager<User> _userManager;
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _current;

    public GetProfileQueryHandler(
        UserManager<User> userManager, IApplicationDbContext db, ICurrentUserService current)
    {
        _userManager = userManager; _db = db; _current = current;
    }

    public async Task<Result<UserProfileDto>> Handle(GetProfileQuery req, CancellationToken ct)
    {
        if (_current.UserId is null) return Result<UserProfileDto>.Fail("ابتدا وارد شوید");

        var user = await _userManager.FindByIdAsync(_current.UserId.Value.ToString());
        if (user is null) return Result<UserProfileDto>.Fail("کاربر یافت نشد");

        // شمارش آدرس و سفارش
        var addressCount = await _db.Addresses
            .CountAsync(a => a.UserId == user.Id, ct);
        var orderCount = await _db.Orders
            .CountAsync(o => o.UserId == user.Id, ct);

        return Result<UserProfileDto>.Ok(new UserProfileDto
        {
            Id = user.Id,
            PhoneNumber = user.PhoneNumber ?? "",
            DisplayUserName = user.DisplayUserName,
            FullName = user.FullName,
            CreatedAt = user.CreatedAt,
            AddressCount = addressCount,
            OrderCount = orderCount
        });
    }
}