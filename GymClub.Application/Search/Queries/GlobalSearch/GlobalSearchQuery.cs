using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using GymClub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Search.Queries.GlobalSearch;

public class GlobalSearchResultDto
{
    public string Query { get; set; } = string.Empty;
    public int TotalResults => Articles.Count + Products.Count;

    public List<ArticleSearchDto> Articles { get; set; } = new();
    public List<ProductSearchDto> Products { get; set; } = new();
}

public class ArticleSearchDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public ArticleType Type { get; set; }
    public string? CategoryTitle { get; set; }
    public DateTime? PublishedAt { get; set; }
}

public class ProductSearchDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? MainImageUrl { get; set; }
    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public string CategoryTitle { get; set; } = string.Empty;
    public decimal FinalPrice => DiscountPrice ?? Price;
    public bool HasDiscount => DiscountPrice.HasValue && DiscountPrice < Price;
    public int Stock { get; set; }
}

public record GlobalSearchQuery(string Query, int PerSectionLimit = 20)
    : IRequest<GlobalSearchResultDto>;

public class GlobalSearchHandler : IRequestHandler<GlobalSearchQuery, GlobalSearchResultDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IImageStorageService _images;

    public GlobalSearchHandler(IApplicationDbContext db, IImageStorageService images)
    {
        _db = db; _images = images;
    }

    public async Task<GlobalSearchResultDto> Handle(GlobalSearchQuery req, CancellationToken ct)
    {
        var q = req.Query?.Trim() ?? string.Empty;
        if (q.Length < 2)
            return new GlobalSearchResultDto { Query = q };

        var result = new GlobalSearchResultDto { Query = q };

        // مقالات
        result.Articles = await _db.Articles.AsNoTracking()
            .Where(a => !a.IsDeleted && a.Status == ArticleStatus.Published
                     && (a.Title.Contains(q) || a.Summary.Contains(q) || a.ContentHtml.Contains(q)))
            .OrderByDescending(a => a.PublishedAt)
            .Take(req.PerSectionLimit)
            .Select(a => new ArticleSearchDto
            {
                Id = a.Id,
                Title = a.Title,
                Slug = a.Slug,
                Summary = a.Summary,
                CoverImageUrl = a.CoverImageUrl,
                Type = a.Type,
                CategoryTitle = a.Category != null ? a.Category.Title : null,
                PublishedAt = a.PublishedAt
            })
            .ToListAsync(ct);

        // محصولات
        result.Products = await _db.Products.AsNoTracking()
            .Where(p => !p.IsDeleted && p.IsActive
                     && (p.Name.Contains(q) || p.Description.Contains(q)))
            .OrderByDescending(p => p.CreatedAt)
            .Take(req.PerSectionLimit)
            .Select(p => new ProductSearchDto
            {
                Id = p.Id,
                Name = p.Name,
                Slug = p.Slug,
                MainImageUrl = p.Images.Where(i => i.IsMain).Select(i => i.Url).FirstOrDefault()
                             ?? p.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault(),
                Price = p.Price,
                DiscountPrice = p.DiscountPrice,
                CategoryTitle = p.Category.Title,
                Stock = p.Stock
            })
            .ToListAsync(ct);

        // تبدیل مسیر عکس‌ها
        foreach (var a in result.Articles)
            a.CoverImageUrl = string.IsNullOrEmpty(a.CoverImageUrl) ? null : _images.GetPublicUrl(a.CoverImageUrl);

        foreach (var p in result.Products)
            p.MainImageUrl = string.IsNullOrEmpty(p.MainImageUrl) ? null : _images.GetPublicUrl(p.MainImageUrl);

        return result;
    }
}