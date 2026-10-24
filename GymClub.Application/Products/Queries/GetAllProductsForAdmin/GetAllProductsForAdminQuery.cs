using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using GymClub.Application.Products.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Products.Queries.GetAllProductsForAdmin;

public record GetAllProductsForAdminQuery(
    string? Search = null,
    Guid? CategoryId = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<PagedResult<ProductCardDto>>;

public class GetAllProductsForAdminQueryHandler
    : IRequestHandler<GetAllProductsForAdminQuery, PagedResult<ProductCardDto>>
{
    private readonly IApplicationDbContext _db;
    public GetAllProductsForAdminQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<ProductCardDto>> Handle(GetAllProductsForAdminQuery req, CancellationToken ct)
    {
        var q = _db.Products.AsNoTracking().Where(p => !p.IsDeleted);
        if (!string.IsNullOrWhiteSpace(req.Search))
            q = q.Where(p => p.Name.Contains(req.Search));
        if (req.CategoryId.HasValue)
            q = q.Where(p => p.CategoryId == req.CategoryId);

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(p => p.CreatedAt)
            .Skip((req.Page - 1) * req.PageSize).Take(req.PageSize)
            .Select(p => new ProductCardDto
            {
                Id = p.Id,
                Name = p.Name,
                Slug = p.Slug,
                MainImageUrl = p.Images.Where(i => i.IsMain).Select(i => i.Url).FirstOrDefault(),
                Price = p.Price,
                DiscountPrice = p.DiscountPrice,
                Stock = p.Stock,
                CategoryTitle = p.Category.Title
            }).ToListAsync(ct);

        return new PagedResult<ProductCardDto>
        {
            Items = items,
            Page = req.Page,
            PageSize = req.PageSize,
            TotalCount = total
        };
    }
}