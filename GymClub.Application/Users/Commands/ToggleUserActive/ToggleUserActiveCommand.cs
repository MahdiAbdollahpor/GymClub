using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using GymClub.Domain.Entities;
using GymClub.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace GymClub.Application.Users.Commands.ToggleUserActive;

public record ToggleUserActiveCommand(Guid UserId) : IRequest<Result<bool>>;

public class ToggleUserActiveCommandHandler
    : IRequestHandler<ToggleUserActiveCommand, Result<bool>>
{
    private readonly UserManager<User> _userManager;
    private readonly ICurrentUserService _current;

    public ToggleUserActiveCommandHandler(
        UserManager<User> userManager, ICurrentUserService current)
    {
        _userManager = userManager;
        _current = current;
    }

    public async Task<Result<bool>> Handle(ToggleUserActiveCommand req, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(req.UserId.ToString());
        if (user is null) return Result<bool>.Fail("کاربر یافت نشد");

        // سوپر ادمین را نمی‌توان غیرفعال کرد
        if (user.Role == UserRole.SuperAdmin)
            return Result<bool>.Fail("سوپر ادمین قابل غیرفعال‌سازی نیست");

        // خودت را نمی‌توانی غیرفعال کنی
        if (_current.UserId == user.Id)
            return Result<bool>.Fail("نمی‌توانید حساب خودتان را غیرفعال کنید");

        user.IsActive = !user.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        var r = await _userManager.UpdateAsync(user);
        return r.Succeeded
            ? Result<bool>.Ok(user.IsActive)
            : Result<bool>.Fail(r.Errors.Select(e => e.Description));
    }
}