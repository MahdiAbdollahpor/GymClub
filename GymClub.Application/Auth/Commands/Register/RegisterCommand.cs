using FluentValidation;
using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using GymClub.Domain.Entities;
using GymClub.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace GymClub.Application.Auth.Commands.Register;

public record RegisterCommand(string PhoneNumber, string DisplayUserName, string Password)
    : IRequest<Result<RegisterResponse>>;

public record RegisterResponse(string PhoneNumber, bool OtpSent);

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .Matches(@"^09\d{9}$").WithMessage("شماره موبایل معتبر نیست (مثال: 09121234567)");
        RuleFor(x => x.DisplayUserName).NotEmpty().MinimumLength(3).MaximumLength(50);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
    }
}

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<RegisterResponse>>
{
    private readonly UserManager<User> _userManager;
    private readonly IOtpService _otp;
    private readonly ISmsService _sms;

    public RegisterCommandHandler(UserManager<User> um, IOtpService otp, ISmsService sms)
    {
        _userManager = um; _otp = otp; _sms = sms;
    }

    public async Task<Result<RegisterResponse>> Handle(RegisterCommand req, CancellationToken ct)
    {
        var existing = await _userManager.FindByNameAsync(req.PhoneNumber);
        if (existing is not null)
            return Result<RegisterResponse>.Fail("این شماره قبلاً ثبت‌نام شده است");

        var user = new User
        {
            UserName = req.PhoneNumber,
            PhoneNumber = req.PhoneNumber,
            DisplayUserName = req.DisplayUserName,
            FullName = req.DisplayUserName,
            PhoneNumberConfirmed = false,
            IsPhoneConfirmed = false,
            Role = UserRole.User
        };

        var result = await _userManager.CreateAsync(user, req.Password);
        if (!result.Succeeded)
            return Result<RegisterResponse>.Fail(result.Errors.Select(e => e.Description));

        await _userManager.AddToRoleAsync(user, "User");

        var otpResult = await _otp.GenerateAsync(req.PhoneNumber, OtpPurpose.Register, ct);
        if (!otpResult.Succeeded)
            return Result<RegisterResponse>.Fail(otpResult.Error!);

        await _sms.SendAsync(req.PhoneNumber, $"کد تأیید ثبت‌نام: {otpResult.Data}", ct);

        return Result<RegisterResponse>.Ok(new RegisterResponse(req.PhoneNumber, true));
    }
}