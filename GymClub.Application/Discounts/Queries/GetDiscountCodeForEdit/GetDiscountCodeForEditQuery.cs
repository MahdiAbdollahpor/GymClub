using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using GymClub.Application.Discounts.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Discounts.Queries.GetDiscountCodeForEdit;

public record GetDiscountCodeForEditQuery(Guid Id) : IRequest<Result<DiscountCodeDto>>;

public class GetDiscountCodeForEditHandler
    : IRequestHandler<GetDiscountCodeForEditQuery, Result<DiscountCodeDto>>
{
    private readonly IApplicationDbContext _db;
    public GetDiscountCodeForEditHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<DiscountCodeDto>> Handle(GetDiscountCodeForEditQuery req, CancellationToken ct)
    {
        var d = await _db.DiscountCodes.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == req.Id && !x.IsDeleted, ct);
        if (d is null) return Result<DiscountCodeDto>.Fail("کد تخفیف یافت نشد");

        return Result<DiscountCodeDto>.Ok(new DiscountCodeDto
        {
            Id = d.Id,
            Code = d.Code,
            Type = d.Type,
            Value = d.Value,
            MaxDiscountAmount = d.MaxDiscountAmount,
            MinOrderAmount = d.MinOrderAmount,
            MaxUsageCount = d.MaxUsageCount,
            UsedCount = d.UsedCount,
            StartsAt = d.StartsAt,
            ExpiresAt = d.ExpiresAt,
            IsActive = d.IsActive,
            CreatedAt = d.CreatedAt
        });
    }
}