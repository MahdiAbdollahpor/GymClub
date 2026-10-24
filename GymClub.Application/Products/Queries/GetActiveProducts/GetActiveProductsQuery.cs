using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using GymClub.Application.Products.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Products.Queries.GetActiveProducts;

public record GetActiveProductsQuery(
    Guid? CategoryId = null,
    string? Search = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    string? Sort = null,   // "price-asc" | "price-desc" | "newest"
    int Page = 1,
    int PageSize = 12
) : IRequest<PagedResult<ProductCardDto>>;

public class GetActiveProductsQueryHandler
    : IRequestHandler<GetActiveProductsQuery, PagedResult<ProductCardDto>>
{
    private readonly IApplicationDbContext _db;

    public GetActiveProductsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<ProductCardDto>> Handle(GetActiveProductsQuery req, CancellationToken ct)
    {
        var q = _db.Products
            .AsNoTracking()
            .Where(p => !p.IsDeleted && p.IsActive);

        if (req.CategoryId.HasValue)
            q = q.Where(p => p.CategoryId == req.CategoryId);

        if (!string.IsNullOrWhiteSpace(req.Search))
            q = q.Where(p => p.Name.Contains(req.Search) || p.Description.Contains(req.Search));

        if (req.MinPrice.HasValue)
            q = q.Where(p => (p.DiscountPrice ?? p.Price) >= req.MinPrice);

        if (req.MaxPrice.HasValue)
            q = q.Where(p => (p.DiscountPrice ?? p.Price) <= req.MaxPrice);

        q = req.Sort switch
        {
            "price-asc" => q.OrderBy(p => p.DiscountPrice ?? p.Price),
            "price-desc" => q.OrderByDescending(p => p.DiscountPrice ?? p.Price),
            _ => q.OrderByDescending(p => p.CreatedAt)
        };

        var total = await q.CountAsync(ct);

        var items = await q
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .Select(p => new ProductCardDto
            {
                Id = p.Id,
                Name = p.Name,
                Slug = p.Slug,
                MainImageUrl = p.Images.Where(i => i.IsMain).Select(i => i.Url).FirstOrDefault()
                            ?? p.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault(),
                Price = p.Price,
                DiscountPrice = p.DiscountPrice,
                Stock = p.Stock,
                CategoryTitle = p.Category.Title
            })
            .ToListAsync(ct);

        return new PagedResult<ProductCardDto>
        {
            Items = items,
            Page = req.Page,
            PageSize = req.PageSize,
            TotalCount = total
        };
    }
}