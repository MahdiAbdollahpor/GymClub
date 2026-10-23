using FluentValidation;
using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using GymClub.Domain.Entities;
using GymClub.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace GymClub.Application.Auth.Commands.ForgotPassword;

public record ForgotPasswordCommand(string PhoneNumber) : IRequest<Result>;

public class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.PhoneNumber).Matches(@"^09\d{9}$");
    }
}

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result>
{
    private readonly UserManager<User> _userManager;
    private readonly IOtpService _otp;
    private readonly ISmsService _sms;

    public ForgotPasswordCommandHandler(UserManager<User> um, IOtpService otp, ISmsService sms)
    {
        _userManager = um; _otp = otp; _sms = sms;
    }

    public async Task<Result> Handle(ForgotPasswordCommand req, CancellationToken ct)
    {
        var user = await _userManager.FindByNameAsync(req.PhoneNumber);
        // حتی اگر کاربر نبود، پیام موفق می‌دهیم (جلوگیری از User Enumeration)
        if (user is null) return Result.Ok();

        var otp = await _otp.GenerateAsync(req.PhoneNumber, OtpPurpose.ResetPassword, ct);
        if (otp.Succeeded)
            await _sms.SendAsync(req.PhoneNumber, $"کد بازیابی رمز: {otp.Data}", ct);

        return Result.Ok();
    }
}

public record ResetPasswordCommand(string PhoneNumber, string Code, string NewPassword)
    : IRequest<Result>;

public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.PhoneNumber).Matches(@"^09\d{9}$");
        RuleFor(x => x.Code).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(6);
    }
}

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result>
{
    private readonly UserManager<User> _userManager;
    private readonly IOtpService _otp;

    public ResetPasswordCommandHandler(UserManager<User> um, IOtpService otp)
    {
        _userManager = um; _otp = otp;
    }

    public async Task<Result> Handle(ResetPasswordCommand req, CancellationToken ct)
    {
        var user = await _userManager.FindByNameAsync(req.PhoneNumber);
        if (user is null) return Result.Fail("اطلاعات نامعتبر");

        var verify = await _otp.VerifyAsync(req.PhoneNumber, req.Code, OtpPurpose.ResetPassword, ct);
        if (!verify.Succeeded) return Result.Fail(verify.Error!);

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, req.NewPassword);
        return result.Succeeded
            ? Result.Ok()
            : Result.Fail(result.Errors.Select(e => e.Description));
    }
}