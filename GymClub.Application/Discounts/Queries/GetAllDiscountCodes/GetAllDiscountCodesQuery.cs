using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using GymClub.Application.Discounts.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Discounts.Queries.GetAllDiscountCodes;

public record GetAllDiscountCodesQuery(
    string? Search = null,
    bool? IsActive = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<PagedResult<DiscountCodeDto>>;

public class GetAllDiscountCodesHandler
    : IRequestHandler<GetAllDiscountCodesQuery, PagedResult<DiscountCodeDto>>
{
    private readonly IApplicationDbContext _db;
    public GetAllDiscountCodesHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<DiscountCodeDto>> Handle(GetAllDiscountCodesQuery req, CancellationToken ct)
    {
        var q = _db.DiscountCodes.AsNoTracking().Where(d => !d.IsDeleted);

        if (!string.IsNullOrWhiteSpace(req.Search))
            q = q.Where(d => d.Code.Contains(req.Search));

        if (req.IsActive.HasValue)
            q = q.Where(d => d.IsActive == req.IsActive);

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(d => d.CreatedAt)
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .Select(d => new DiscountCodeDto
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
            })
            .ToListAsync(ct);

        return new PagedResult<DiscountCodeDto>
        {
            Items = items,
            Page = req.Page,
            PageSize = req.PageSize,
            TotalCount = total
        };
    }
}