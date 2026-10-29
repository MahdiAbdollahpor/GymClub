using FluentValidation;
using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using GymClub.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace GymClub.Application.Users.Commands.UpdateProfile;

public record UpdateProfileCommand(string DisplayUserName, string FullName)
    : IRequest<Result>;

public class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.DisplayUserName).NotEmpty().MinimumLength(3).MaximumLength(50);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
    }
}

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, Result>
{
    private readonly UserManager<User> _userManager;
    private readonly ICurrentUserService _current;

    public UpdateProfileCommandHandler(UserManager<User> userManager, ICurrentUserService current)
    {
        _userManager = userManager; _current = current;
    }

    public async Task<Result> Handle(UpdateProfileCommand req, CancellationToken ct)
    {
        if (_current.UserId is null) return Result.Fail("ابتدا وارد شوید");

        var user = await _userManager.FindByIdAsync(_current.UserId.Value.ToString());
        if (user is null) return Result.Fail("کاربر یافت نشد");

        user.DisplayUserName = req.DisplayUserName.Trim();
        user.FullName = req.FullName.Trim();
        user.UpdatedAt = DateTime.UtcNow;

        var r = await _userManager.UpdateAsync(user);
        return r.Succeeded ? Result.Ok() : Result.Fail(r.Errors.Select(e => e.Description));
    }
}