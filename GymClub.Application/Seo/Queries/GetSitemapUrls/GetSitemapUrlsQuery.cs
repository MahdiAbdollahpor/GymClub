using GymClub.Application.Common.Interfaces;
using GymClub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Seo.Queries.GetSitemapUrls;

public class SitemapUrl
{
    public string Location { get; set; } = string.Empty;
    public DateTime? LastModified { get; set; }
    public string? ChangeFreq { get; set; }
    public decimal? Priority { get; set; }
}

public record GetSitemapUrlsQuery(string BaseUrl) : IRequest<List<SitemapUrl>>;

public class GetSitemapUrlsHandler : IRequestHandler<GetSitemapUrlsQuery, List<SitemapUrl>>
{
    private readonly IApplicationDbContext _db;
    public GetSitemapUrlsHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<SitemapUrl>> Handle(GetSitemapUrlsQuery req, CancellationToken ct)
    {
        var baseUrl = req.BaseUrl.TrimEnd('/');
        var urls = new List<SitemapUrl>
        {
            new() { Location = $"{baseUrl}/", ChangeFreq = "daily", Priority = 1.0m },
            new() { Location = $"{baseUrl}/about", ChangeFreq = "monthly", Priority = 0.8m },
            new() { Location = $"{baseUrl}/contact", ChangeFreq = "monthly", Priority = 0.7m },
            new() { Location = $"{baseUrl}/articles", ChangeFreq = "daily", Priority = 0.9m },
            new() { Location = $"{baseUrl}/products", ChangeFreq = "daily", Priority = 0.9m },
            new() { Location = $"{baseUrl}/workout", ChangeFreq = "weekly", Priority = 0.8m },
        };

        // مقالات
        var articles = await _db.Articles.AsNoTracking()
            .Where(a => !a.IsDeleted && a.Status == ArticleStatus.Published)
            .Select(a => new { a.Slug, a.UpdatedAt, a.PublishedAt, a.CreatedAt })
            .ToListAsync(ct);

        urls.AddRange(articles.Select(a => new SitemapUrl
        {
            Location = $"{baseUrl}/articles/{a.Slug}",
            LastModified = a.UpdatedAt ?? a.PublishedAt ?? a.CreatedAt,
            ChangeFreq = "weekly",
            Priority = 0.7m
        }));

        // محصولات
        var products = await _db.Products.AsNoTracking()
            .Where(p => !p.IsDeleted && p.IsActive)
            .Select(p => new { p.Slug, p.UpdatedAt, p.CreatedAt })
            .ToListAsync(ct);

        urls.AddRange(products.Select(p => new SitemapUrl
        {
            Location = $"{baseUrl}/products/{p.Slug}",
            LastModified = p.UpdatedAt ?? p.CreatedAt,
            ChangeFreq = "weekly",
            Priority = 0.7m
        }));

        return urls;
    }
}