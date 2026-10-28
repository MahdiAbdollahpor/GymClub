using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using GymClub.Application.Products.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.ProductCategories.Queries.GetProductCategoryDetails;

public class ProductCategoryDetailsDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public string? ParentTitle { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<ProductCardDto> Products { get; set; } = new();
}

public record GetProductCategoryDetailsQuery(Guid Id) : IRequest<Result<ProductCategoryDetailsDto>>;

public class GetProductCategoryDetailsQueryHandler
    : IRequestHandler<GetProductCategoryDetailsQuery, Result<ProductCategoryDetailsDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IImageStorageService _images;

    public GetProductCategoryDetailsQueryHandler(IApplicationDbContext db, IImageStorageService images)
    {
        _db = db; _images = images;
    }

    public async Task<Result<ProductCategoryDetailsDto>> Handle(
        GetProductCategoryDetailsQuery req, CancellationToken ct)
    {
        var c = await _db.ProductCategories
            .AsNoTracking()
            .Include(x => x.Parent)
            .Include(x => x.Products).ThenInclude(p => p.Images)
            .FirstOrDefaultAsync(x => x.Id == req.Id && !x.IsDeleted, ct);

        if (c is null) return Result<ProductCategoryDetailsDto>.Fail("دسته یافت نشد");

        var products = c.Products
            .Where(p => !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new ProductCardDto
            {
                Id = p.Id,
                Name = p.Name,
                Slug = p.Slug,
                Price = p.Price,
                DiscountPrice = p.DiscountPrice,
                Stock = p.Stock,
                CategoryTitle = c.Title,
                MainImageUrl = p.Images.Where(i => i.IsMain).Select(i => i.Url).FirstOrDefault()
            }).ToList();

        return Result<ProductCategoryDetailsDto>.Ok(new ProductCategoryDetailsDto
        {
            Id = c.Id,
            Title = c.Title,
            Slug = c.Slug,
            ParentId = c.ParentId,
            ParentTitle = c.Parent?.Title,
            ImageUrl = c.ImageUrl,
            IsActive = c.IsActive,
            SortOrder = c.SortOrder,
            CreatedAt = c.CreatedAt,
            Products = products
        });
    }
}