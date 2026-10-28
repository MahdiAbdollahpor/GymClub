using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using GymClub.Application.Products.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Products.Queries.GetProductForEdit;

public record GetProductForEditQuery(Guid Id) : IRequest<Result<ProductDetailsDto>>;

public class GetProductForEditQueryHandler
    : IRequestHandler<GetProductForEditQuery, Result<ProductDetailsDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IImageStorageService _images;

    public GetProductForEditQueryHandler(IApplicationDbContext db, IImageStorageService images)
    {
        _db = db; _images = images;
    }

    public async Task<Result<ProductDetailsDto>> Handle(GetProductForEditQuery req, CancellationToken ct)
    {
        var p = await _db.Products.AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Images)
            .Include(x => x.Attributes)
            .FirstOrDefaultAsync(x => x.Id == req.Id && !x.IsDeleted, ct);
        if (p is null) return Result<ProductDetailsDto>.Fail("محصول یافت نشد");

        return Result<ProductDetailsDto>.Ok(new ProductDetailsDto
        {
            Id = p.Id,
            Name = p.Name,
            Slug = p.Slug,
            Description = p.Description,
            CategoryId = p.CategoryId,
            CategoryTitle = p.Category.Title,
            Price = p.Price,
            IsActive = p.IsActive,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt,
            DiscountPrice = p.DiscountPrice,
            Stock = p.Stock,
            Images = p.Images.OrderBy(i => i.SortOrder).Select(i => new ProductImageDto
            {
                Id = i.Id,
                Url = _images.GetPublicUrl(i.Url),
                SortOrder = i.SortOrder,
                IsMain = i.IsMain
            }).ToList(),
            Attributes = p.Attributes.OrderBy(a => a.SortOrder).Select(a => new ProductAttributeDto
            {
                Id = a.Id,
                Title = a.Title,
                Value = a.Value,
                SortOrder = a.SortOrder
            }).ToList()
        });
    }
}