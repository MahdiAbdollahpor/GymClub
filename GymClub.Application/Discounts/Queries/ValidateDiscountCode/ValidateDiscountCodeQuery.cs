using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using GymClub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Discounts.Queries.ValidateDiscountCode;

public record ValidateDiscountCodeQuery(string Code, decimal SubTotal)
    : IRequest<Result<DiscountValidationResult>>;

public class DiscountValidationResult
{
    public Guid DiscountCodeId { get; set; }
    public string Code { get; set; } = string.Empty;
    public decimal DiscountAmount { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class ValidateDiscountCodeQueryHandler
    : IRequestHandler<ValidateDiscountCodeQuery, Result<DiscountValidationResult>>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public ValidateDiscountCodeQueryHandler(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db; _clock = clock;
    }

    public async Task<Result<DiscountValidationResult>> Handle(ValidateDiscountCodeQuery req, CancellationToken ct)
    {
        var now = _clock.UtcNow;
        var code = req.Code.Trim().ToUpperInvariant();

        var dc = await _db.DiscountCodes
            .FirstOrDefaultAsync(d => d.Code.ToUpper() == code && !d.IsDeleted && d.IsActive, ct);
        if (dc is null) return Result<DiscountValidationResult>.Fail("کد تخفیف معتبر نیست");
        if (dc.StartsAt.HasValue && dc.StartsAt > now) return Result<DiscountValidationResult>.Fail("کد هنوز فعال نشده است");
        if (dc.ExpiresAt.HasValue && dc.ExpiresAt < now) return Result<DiscountValidationResult>.Fail("کد منقضی شده است");
        if (dc.MaxUsageCount.HasValue && dc.UsedCount >= dc.MaxUsageCount) return Result<DiscountValidationResult>.Fail("ظرفیت استفاده از کد پر شده است");
        if (dc.MinOrderAmount.HasValue && req.SubTotal < dc.MinOrderAmount) return Result<DiscountValidationResult>.Fail($"حداقل مبلغ سفارش {dc.MinOrderAmount.Value:N0} تومان است");

        decimal discount;
        if (dc.Type == DiscountType.Percentage)
        {
            discount = req.SubTotal * dc.Value / 100m;
            if (dc.MaxDiscountAmount.HasValue && discount > dc.MaxDiscountAmount.Value)
                discount = dc.MaxDiscountAmount.Value;
        }
        else
        {
            discount = dc.Value;
        }
        if (discount > req.SubTotal) discount = req.SubTotal;

        return Result<DiscountValidationResult>.Ok(new DiscountValidationResult
        {
            DiscountCodeId = dc.Id,
            Code = dc.Code,
            DiscountAmount = discount,
            Description = dc.Type == DiscountType.Percentage
                ? $"تخفیف {dc.Value:N0}٪"
                : $"تخفیف {dc.Value:N0} تومان"
        });
    }
}