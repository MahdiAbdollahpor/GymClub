using GymClub.Application.Articles.Dtos;
using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using GymClub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Articles.Queries.GetAllArticlesForAdmin;

public record GetAllArticlesForAdminQuery(
    string? Search = null,
    ArticleStatus? Status = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<PagedResult<ArticleCardDto>>;

public class GetAllArticlesForAdminQueryHandler
    : IRequestHandler<GetAllArticlesForAdminQuery, PagedResult<ArticleCardDto>>
{
    private readonly IApplicationDbContext _db;

    public GetAllArticlesForAdminQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<ArticleCardDto>> Handle(GetAllArticlesForAdminQuery req, CancellationToken ct)
    {
        var q = _db.Articles.AsNoTracking().Where(a => !a.IsDeleted);

        if (!string.IsNullOrWhiteSpace(req.Search))
            q = q.Where(a => a.Title.Contains(req.Search) || a.Summary.Contains(req.Search));

        if (req.Status.HasValue)
            q = q.Where(a => a.Status == req.Status);

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(a => a.CreatedAt)
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
                ViewCount = a.ViewCount
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