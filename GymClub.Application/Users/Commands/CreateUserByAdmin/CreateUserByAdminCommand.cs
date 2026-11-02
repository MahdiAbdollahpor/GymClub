using FluentValidation;
using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using GymClub.Domain.Entities;
using GymClub.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace GymClub.Application.Users.Commands.CreateUserByAdmin;

public record CreateUserByAdminCommand(
    string PhoneNumber,
    string DisplayUserName,
    string FullName,
    string Password,
    UserRole Role
) : IRequest<Result<Guid>>;

public class CreateUserByAdminValidator : AbstractValidator<CreateUserByAdminCommand>
{
    public CreateUserByAdminValidator()
    {
        RuleFor(x => x.PhoneNumber).NotEmpty().Matches(@"^09\d{9}$");
        RuleFor(x => x.DisplayUserName).NotEmpty().MinimumLength(3).MaximumLength(50);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
        RuleFor(x => x.Role).Must(r => r != UserRole.SuperAdmin)
            .WithMessage("ساخت سوپر ادمین از این فرم مجاز نیست");
    }
}

public class CreateUserByAdminCommandHandler
    : IRequestHandler<CreateUserByAdminCommand, Result<Guid>>
{
    private readonly UserManager<User> _userManager;
    private readonly ICurrentUserService _current;

    public CreateUserByAdminCommandHandler(
        UserManager<User> userManager, ICurrentUserService current)
    {
        _userManager = userManager;
        _current = current;
    }

    public async Task<Result<Guid>> Handle(CreateUserByAdminCommand req, CancellationToken ct)
    {
        // فقط سوپر ادمین می‌تواند کاربر بسازد
        if (!_current.IsInRole("SuperAdmin"))
            return Result<Guid>.Fail("فقط سوپر ادمین می‌تواند کاربر جدید بسازد");

        // نقش سوپر ادمین را نمی‌شود ساخت
        if (req.Role == UserRole.SuperAdmin)
            return Result<Guid>.Fail("ساخت سوپر ادمین جدید مجاز نیست");

        var exists = await _userManager.FindByNameAsync(req.PhoneNumber);
        if (exists is not null) return Result<Guid>.Fail("این شماره قبلاً ثبت‌نام شده است");

        var user = new User
        {
            UserName = req.PhoneNumber,
            PhoneNumber = req.PhoneNumber,
            PhoneNumberConfirmed = true,
            IsPhoneConfirmed = true,   // ← بدون نیاز به OTP
            DisplayUserName = req.DisplayUserName.Trim(),
            FullName = req.FullName.Trim(),
            Role = req.Role,
            IsActive = true
        };

        var r = await _userManager.CreateAsync(user, req.Password);
        if (!r.Succeeded)
            return Result<Guid>.Fail(r.Errors.Select(e => e.Description));

        var roleName = req.Role switch
        {
            UserRole.SuperAdmin => "SuperAdmin",
            UserRole.Admin => "Admin",
            UserRole.ContentWriter => "ContentWriter",
            UserRole.ShopManager => "ShopManager",
            UserRole.Trainer => "Trainer",
            UserRole.User => "User",
            _ => "User"
        };
        await _userManager.AddToRoleAsync(user, roleName);

        return Result<Guid>.Ok(user.Id);
    }
}