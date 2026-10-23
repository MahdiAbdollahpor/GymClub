using GymClub.Application.Articles.Dtos;
using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using GymClub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Articles.Queries.GetPublishedArticles;

public record GetPublishedArticlesQuery(
    Guid? CategoryId = null,
    ArticleType? Type = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 9
) : IRequest<PagedResult<ArticleCardDto>>;

public class GetPublishedArticlesQueryHandler
    : IRequestHandler<GetPublishedArticlesQuery, PagedResult<ArticleCardDto>>
{
    private readonly IApplicationDbContext _db;

    public GetPublishedArticlesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<ArticleCardDto>> Handle(GetPublishedArticlesQuery req, CancellationToken ct)
    {
        var query = _db.Articles
            .AsNoTracking()
            .Where(a => !a.IsDeleted && a.Status == ArticleStatus.Published);

        if (req.CategoryId.HasValue)
            query = query.Where(a => a.CategoryId == req.CategoryId);

        if (req.Type.HasValue)
            query = query.Where(a => a.Type == req.Type);

        if (!string.IsNullOrWhiteSpace(req.Search))
            query = query.Where(a => a.Title.Contains(req.Search) || a.Summary.Contains(req.Search));

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(a => a.PublishedAt ?? a.CreatedAt)
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
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

        return new PagedResult<ArticleCardDto>
        {
            Items = items,
            Page = req.Page,
            PageSize = req.PageSize,
            TotalCount = total
        };
    }
}