using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using GymClub.Application.Products.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.ProductCategories.Queries.GetForEdit;

public record GetProductCategoryForEditQuery(Guid Id) : IRequest<Result<ProductCategoryDto>>;

public class GetProductCategoryForEditQueryHandler
    : IRequestHandler<GetProductCategoryForEditQuery, Result<ProductCategoryDto>>
{
    private readonly IApplicationDbContext _db;
    public GetProductCategoryForEditQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<ProductCategoryDto>> Handle(GetProductCategoryForEditQuery req, CancellationToken ct)
    {
        var c = await _db.ProductCategories.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == req.Id && !x.IsDeleted, ct);
        if (c is null) return Result<ProductCategoryDto>.Fail("دسته یافت نشد");

        return Result<ProductCategoryDto>.Ok(new ProductCategoryDto
        {
            Id = c.Id,
            Title = c.Title,
            Slug = c.Slug,
            ParentId = c.ParentId,
            ImageUrl = c.ImageUrl,
            IsActive = c.IsActive,
            SortOrder = c.SortOrder
        });
    }
}