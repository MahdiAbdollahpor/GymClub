using FluentValidation;
using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using GymClub.Domain.Entities;
using GymClub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Discounts.Commands.CreateDiscountCode;

public record CreateDiscountCodeCommand(
    string Code,
    int Type,
    decimal Value,
    decimal? MaxDiscountAmount,
    decimal? MinOrderAmount,
    int? MaxUsageCount,
    DateTime? StartsAt,
    DateTime? ExpiresAt,
    bool IsActive
) : IRequest<Result<Guid>>;

public class CreateDiscountCodeValidator : AbstractValidator<CreateDiscountCodeCommand>
{
    public CreateDiscountCodeValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50)
            .Matches(@"^[A-Za-z0-9_-]+$").WithMessage("کد فقط می‌تواند شامل حروف، اعداد، - و _ باشد");
        RuleFor(x => x.Value).GreaterThan(0);
        RuleFor(x => x.Type).Must(t => t == 0 || t == 1)
    .WithMessage("نوع تخفیف نامعتبر است");
        RuleFor(x => x.Value).LessThanOrEqualTo(100)
    .When(x => x.Type == 1)
    .WithMessage("درصد تخفیف نمی‌تواند بیشتر از ۱۰۰ باشد");
        RuleFor(x => x.MaxUsageCount).GreaterThan(0).When(x => x.MaxUsageCount.HasValue);
        RuleFor(x => x.ExpiresAt).GreaterThan(x => x.StartsAt)
            .When(x => x.ExpiresAt.HasValue && x.StartsAt.HasValue)
            .WithMessage("تاریخ پایان باید بعد از تاریخ شروع باشد");
    }
}

public class CreateDiscountCodeHandler : IRequestHandler<CreateDiscountCodeCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    public CreateDiscountCodeHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<Guid>> Handle(CreateDiscountCodeCommand req, CancellationToken ct)
    {
        var code = req.Code.Trim().ToUpperInvariant();

        if (await _db.DiscountCodes.AnyAsync(d => d.Code.ToUpper() == code && !d.IsDeleted, ct))
            return Result<Guid>.Fail("این کد تخفیف قبلاً ثبت شده است");

        var discountType = (DiscountType)req.Type;
        if (discountType != DiscountType.Percentage && discountType != DiscountType.FixedAmount)
            return Result<Guid>.Fail("نوع تخفیف نامعتبر است");

        var entity = new DiscountCode
        {
            Code = code,
            Type = discountType,
            Value = req.Value,
            MaxDiscountAmount = req.MaxDiscountAmount,
            MinOrderAmount = req.MinOrderAmount,
            MaxUsageCount = req.MaxUsageCount,
            StartsAt = req.StartsAt,
            ExpiresAt = req.ExpiresAt,
            IsActive = req.IsActive
        };
        _db.DiscountCodes.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Result<Guid>.Ok(entity.Id);
    }
}