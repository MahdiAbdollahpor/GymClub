using FluentValidation;
using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using GymClub.Domain.Entities;
using GymClub.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Users.Commands.ChangeUserRole;

public record ChangeUserRoleCommand(Guid UserId, UserRole NewRole) : IRequest<Result>;

public class ChangeUserRoleCommandValidator : AbstractValidator<ChangeUserRoleCommand>
{
    public ChangeUserRoleCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.NewRole).IsInEnum();
    }
}

public class ChangeUserRoleCommandHandler : IRequestHandler<ChangeUserRoleCommand, Result>
{
    private readonly UserManager<User> _userManager;
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _current;

    public ChangeUserRoleCommandHandler(
        UserManager<User> userManager,
        IApplicationDbContext db,
        ICurrentUserService current)
    {
        _userManager = userManager;
        _db = db;
        _current = current;
    }



    public async Task<Result> Handle(ChangeUserRoleCommand req, CancellationToken ct)
    {
        if (!_current.IsInRole("SuperAdmin"))
            return Result.Fail("فقط سوپر ادمین می‌تواند نقش کاربران را تغییر دهد");

        // ⚠️ از IApplicationDbContext استفاده کن (نه UserManager) تا Role مستقیماً ذخیره شود
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == req.UserId, ct);
        if (user is null) return Result.Fail("کاربر یافت نشد");

        if (user.Role == UserRole.SuperAdmin)
            return Result.Fail("نقش سوپر ادمین قابل تغییر نیست");

        if (req.NewRole == UserRole.SuperAdmin)
            return Result.Fail("ساخت سوپر ادمین جدید مجاز نیست");

        if (_current.UserId == user.Id)
            return Result.Fail("نمی‌توانید نقش خودتان را تغییر دهید");

        var oldRoleName = MapRoleName(user.Role);
        var newRoleName = MapRoleName(req.NewRole);

        if (oldRoleName != newRoleName)
        {
            // ۱. به‌روزرسانی جدول AspNetUserRoles (نقش‌های Identity)
            if (!string.IsNullOrEmpty(oldRoleName) && await _userManager.IsInRoleAsync(user, oldRoleName))
                await _userManager.RemoveFromRoleAsync(user, oldRoleName);

            if (!string.IsNullOrEmpty(newRoleName) && !await _userManager.IsInRoleAsync(user, newRoleName))
                await _userManager.AddToRoleAsync(user, newRoleName);
        }

        // ۲. به‌روزرسانی ستون Role در جدول Users (مستقیم با EF)
        user.Role = req.NewRole;
        user.UpdatedAt = DateTime.UtcNow;
        _db.Users.Update(user);
        await _db.SaveChangesAsync(ct);
        await _userManager.UpdateSecurityStampAsync(user);

        return Result.Ok();
    }

    private static string MapRoleName(UserRole role) => role switch
    {
        UserRole.SuperAdmin => "SuperAdmin",
        UserRole.Admin => "Admin",
        UserRole.ContentWriter => "ContentWriter",
        UserRole.ShopManager => "ShopManager",
        UserRole.Trainer => "Trainer",
        UserRole.User => "User",
        _ => "User"
    };
}