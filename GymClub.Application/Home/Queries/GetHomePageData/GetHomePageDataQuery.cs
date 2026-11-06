using GymClub.Application.Articles.Dtos;
using GymClub.Application.Common.Interfaces;
using GymClub.Application.Products.Dtos;
using GymClub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Home.Queries.GetHomePageData;

public class HomePageDataDto
{
    public List<ArticleCardDto> LatestArticles { get; set; } = new();
    public List<ProductCardDto> FeaturedProducts { get; set; } = new();
    public List<ProductCardDto> NewProducts { get; set; } = new();
    public List<ProductCategoryDto> TopCategories { get; set; } = new();
}

public record GetHomePageDataQuery : IRequest<HomePageDataDto>;

public class GetHomePageDataHandler : IRequestHandler<GetHomePageDataQuery, HomePageDataDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IImageStorageService _images;

    public GetHomePageDataHandler(IApplicationDbContext db, IImageStorageService images)
    {
        _db = db; _images = images;
    }

    public async Task<HomePageDataDto> Handle(GetHomePageDataQuery req, CancellationToken ct)
    {
        var dto = new HomePageDataDto();

        // آخرین مقالات منتشرشده
        dto.LatestArticles = await _db.Articles.AsNoTracking()
            .Where(a => !a.IsDeleted && a.Status == ArticleStatus.Published)
            .OrderByDescending(a => a.PublishedAt ?? a.CreatedAt)
            .Take(3)
            .Select(a => new ArticleCardDto
            {
                Id = a.Id,
                Title = a.Title,
                Slug = a.Slug,
                Summary = a.Summary,
                CoverImageUrl = a.CoverImageUrl,
                Type = a.Type,
                CategoryTitle = a.Category != null ? a.Category.Title : null,
                PublishedAt = a.PublishedAt,
                ViewCount = a.ViewCount,
                HasVideo = a.VideoUrl != null
            })
            .ToListAsync(ct);

        // محصولات دارای تخفیف (Featured)
        dto.FeaturedProducts = await _db.Products.AsNoTracking()
            .Where(p => !p.IsDeleted && p.IsActive && p.Stock > 0 && p.DiscountPrice != null)
            .OrderByDescending(p => p.CreatedAt)
            .Take(4)
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

        // محصولات جدید (اگر Featured کمتر از ۴ بود)
        if (dto.FeaturedProducts.Count < 4)
        {
            var excludeIds = dto.FeaturedProducts.Select(p => p.Id).ToList();
            dto.NewProducts = await _db.Products.AsNoTracking()
                .Where(p => !p.IsDeleted && p.IsActive && p.Stock > 0 && !excludeIds.Contains(p.Id))
                .OrderByDescending(p => p.CreatedAt)
                .Take(4 - dto.FeaturedProducts.Count)
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
        }

        // دسته‌بندی‌های برتر
        dto.TopCategories = await _db.ProductCategories.AsNoTracking()
            .Where(c => !c.IsDeleted && c.IsActive)
            .OrderBy(c => c.SortOrder)
            .Take(4)
            .Select(c => new ProductCategoryDto
            {
                Id = c.Id,
                Title = c.Title,
                Slug = c.Slug,
                IsActive = c.IsActive,
                SortOrder = c.SortOrder,
                ProductCount = c.Products.Count(p => !p.IsDeleted && p.IsActive)
            })
            .ToListAsync(ct);

        // تبدیل مسیر عکس‌ها
        foreach (var a in dto.LatestArticles)
            a.CoverImageUrl = string.IsNullOrEmpty(a.CoverImageUrl) ? null : _images.GetPublicUrl(a.CoverImageUrl);
        foreach (var p in dto.FeaturedProducts.Concat(dto.NewProducts))
            p.MainImageUrl = string.IsNullOrEmpty(p.MainImageUrl) ? null : _images.GetPublicUrl(p.MainImageUrl);

        return dto;
    }
}