using FluentValidation;
using GymClub.Application.Common.Models;
using GymClub.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace GymClub.Application.Auth.Commands.Login;

public record LoginCommand(string PhoneNumber, string Password, bool RememberMe)
    : IRequest<Result<Guid>>;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.PhoneNumber).Matches(@"^09\d{9}$");
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<Guid>>
{
    private readonly UserManager<User> _userManager;

    public LoginCommandHandler(UserManager<User> um) => _userManager = um;

    public async Task<Result<Guid>> Handle(LoginCommand req, CancellationToken ct)
    {
        var user = await _userManager.FindByNameAsync(req.PhoneNumber);
        if (user is null) return Result<Guid>.Fail("شماره یا رمز عبور اشتباه است");
        if (!user.IsPhoneConfirmed) return Result<Guid>.Fail("شماره تأیید نشده است");

        var ok = await _userManager.CheckPasswordAsync(user, req.Password);
        if (!ok) return Result<Guid>.Fail("شماره یا رمز عبور اشتباه است");

        return Result<Guid>.Ok(user.Id);
    }
}