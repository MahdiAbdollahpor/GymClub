using GymClub.Application.Articles.Dtos;
using GymClub.Application.Common.Interfaces;
using GymClub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Articles.Queries.GetArticleCategories;

public record GetArticleCategoriesQuery : IRequest<List<ArticleCategoryDto>>;

public class GetArticleCategoriesQueryHandler
    : IRequestHandler<GetArticleCategoriesQuery, List<ArticleCategoryDto>>
{
    private readonly IApplicationDbContext _db;

    public GetArticleCategoriesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<ArticleCategoryDto>> Handle(GetArticleCategoriesQuery req, CancellationToken ct)
    {
        return await _db.ArticleCategories
            .AsNoTracking()
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Title)
            .Select(c => new ArticleCategoryDto
            {
                Id = c.Id,
                Title = c.Title,
                Slug = c.Slug,
                ParentId = c.ParentId,
                SortOrder = c.SortOrder,
                ArticleCount = c.Articles.Count(a => !a.IsDeleted && a.Status == ArticleStatus.Published)
            })
            .ToListAsync(ct);
    }
}