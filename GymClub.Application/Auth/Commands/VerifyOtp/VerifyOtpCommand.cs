using FluentValidation;
using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using GymClub.Domain.Entities;
using GymClub.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace GymClub.Application.Auth.Commands.VerifyOtp;

public record VerifyOtpCommand(string PhoneNumber, string Code)
    : IRequest<Result<Guid>>;

public class VerifyOtpCommandValidator : AbstractValidator<VerifyOtpCommand>
{
    public VerifyOtpCommandValidator()
    {
        RuleFor(x => x.PhoneNumber).Matches(@"^09\d{9}$");
        RuleFor(x => x.Code).NotEmpty();
    }
}

public class VerifyOtpCommandHandler : IRequestHandler<VerifyOtpCommand, Result<Guid>>
{
    private readonly UserManager<User> _userManager;
    private readonly IOtpService _otp;

    public VerifyOtpCommandHandler(UserManager<User> um, IOtpService otp)
    {
        _userManager = um; _otp = otp;
    }

    public async Task<Result<Guid>> Handle(VerifyOtpCommand req, CancellationToken ct)
    {
        var verify = await _otp.VerifyAsync(req.PhoneNumber, req.Code, OtpPurpose.Register, ct);
        if (!verify.Succeeded) return Result<Guid>.Fail(verify.Error!);

        var user = await _userManager.FindByNameAsync(req.PhoneNumber);
        if (user is null) return Result<Guid>.Fail("کاربر یافت نشد");

        user.IsPhoneConfirmed = true;
        user.PhoneNumberConfirmed = true;
        await _userManager.UpdateAsync(user);

        return Result<Guid>.Ok(user.Id);
    }
}