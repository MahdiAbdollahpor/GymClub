using FluentValidation;
using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using GymClub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Discounts.Commands.UpdateDiscountCode;

public record UpdateDiscountCodeCommand(
    Guid Id,
    string Code,
    int Type,
    decimal Value,
    decimal? MaxDiscountAmount,
    decimal? MinOrderAmount,
    int? MaxUsageCount,
    DateTime? StartsAt,
    DateTime? ExpiresAt,
    bool IsActive
) : IRequest<Result>;

public class UpdateDiscountCodeValidator : AbstractValidator<UpdateDiscountCodeCommand>
{
    public UpdateDiscountCodeValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50)
            .Matches(@"^[A-Za-z0-9_-]+$").WithMessage("کد فقط می‌تواند شامل حروف، اعداد، - و _ باشد");
        RuleFor(x => x.Value).GreaterThan(0);
        RuleFor(x => x.Type).Must(t => t == 0 || t == 1)
     .WithMessage("نوع تخفیف نامعتبر است");

        RuleFor(x => x.Value).LessThanOrEqualTo(100)
            .When(x => x.Type == 1)
            .WithMessage("درصد تخفیف نمی‌تواند بیشتر از ۱۰۰ باشد");
    }
}

public class UpdateDiscountCodeHandler : IRequestHandler<UpdateDiscountCodeCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;
    public UpdateDiscountCodeHandler(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db; _clock = clock;
    }

    public async Task<Result> Handle(UpdateDiscountCodeCommand req, CancellationToken ct)
    {
        var entity = await _db.DiscountCodes.FirstOrDefaultAsync(d => d.Id == req.Id && !d.IsDeleted, ct);
        if (entity is null) return Result.Fail("کد تخفیف یافت نشد");

        var code = req.Code.Trim().ToUpperInvariant();
        if (await _db.DiscountCodes.AnyAsync(d => d.Code.ToUpper() == code && d.Id != req.Id && !d.IsDeleted, ct))
            return Result.Fail("این کد تخفیف قبلاً ثبت شده است");

        var discountType = (DiscountType)req.Type;
        if (discountType != DiscountType.Percentage && discountType != DiscountType.FixedAmount)
            return Result<Guid>.Fail("نوع تخفیف نامعتبر است");

        entity.Code = code;
        entity.Type = discountType;
        entity.Value = req.Value;
        entity.MaxDiscountAmount = req.MaxDiscountAmount;
        entity.MinOrderAmount = req.MinOrderAmount;
        entity.MaxUsageCount = req.MaxUsageCount;
        entity.StartsAt = req.StartsAt;
        entity.ExpiresAt = req.ExpiresAt;
        entity.IsActive = req.IsActive;
        entity.UpdatedAt = _clock.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}