using FluentValidation;
using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using GymClub.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace GymClub.Application.Users.Commands.ChangePassword;

public record ChangePasswordCommand(string CurrentPassword, string NewPassword)
    : IRequest<Result>;

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(6);
    }
}

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result>
{
    private readonly UserManager<User> _userManager;
    private readonly ICurrentUserService _current;

    public ChangePasswordCommandHandler(UserManager<User> userManager, ICurrentUserService current)
    {
        _userManager = userManager; _current = current;
    }

    public async Task<Result> Handle(ChangePasswordCommand req, CancellationToken ct)
    {
        if (_current.UserId is null) return Result.Fail("ابتدا وارد شوید");

        var user = await _userManager.FindByIdAsync(_current.UserId.Value.ToString());
        if (user is null) return Result.Fail("کاربر یافت نشد");

        var r = await _userManager.ChangePasswordAsync(user, req.CurrentPassword, req.NewPassword);
        return r.Succeeded ? Result.Ok() : Result.Fail(r.Errors.Select(e => e.Description));
    }
}