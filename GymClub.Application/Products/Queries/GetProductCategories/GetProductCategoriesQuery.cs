using GymClub.Application.Common.Interfaces;
using GymClub.Application.Products.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Products.Queries.GetProductCategories;

public record GetProductCategoriesQuery(bool OnlyActive = true)
    : IRequest<List<ProductCategoryDto>>;

public class GetProductCategoriesQueryHandler
    : IRequestHandler<GetProductCategoriesQuery, List<ProductCategoryDto>>
{
    private readonly IApplicationDbContext _db;
    public GetProductCategoriesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<ProductCategoryDto>> Handle(GetProductCategoriesQuery req, CancellationToken ct)
    {
        var q = _db.ProductCategories.AsNoTracking().Where(c => !c.IsDeleted);
        if (req.OnlyActive) q = q.Where(c => c.IsActive);

        return await q
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Title)
            .Select(c => new ProductCategoryDto
            {
                Id = c.Id,
                Title = c.Title,
                Slug = c.Slug,
                ParentId = c.ParentId,
                ImageUrl = c.ImageUrl,
                IsActive = c.IsActive,
                SortOrder = c.SortOrder,
                ProductCount = c.Products.Count(p => !p.IsDeleted && p.IsActive)
            })
            .ToListAsync(ct);
    }
}