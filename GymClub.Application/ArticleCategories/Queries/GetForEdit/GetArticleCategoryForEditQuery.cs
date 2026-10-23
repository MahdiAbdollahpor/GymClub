using GymClub.Application.Articles.Dtos;
using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.ArticleCategories.Queries.GetForEdit;

public record GetArticleCategoryForEditQuery(Guid Id) : IRequest<Result<ArticleCategoryDto>>;

public class GetArticleCategoryForEditQueryHandler
    : IRequestHandler<GetArticleCategoryForEditQuery, Result<ArticleCategoryDto>>
{
    private readonly IApplicationDbContext _db;

    public GetArticleCategoryForEditQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<ArticleCategoryDto>> Handle(GetArticleCategoryForEditQuery req, CancellationToken ct)
    {
        var c = await _db.ArticleCategories.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == req.Id && !x.IsDeleted, ct);
        if (c is null) return Result<ArticleCategoryDto>.Fail("دسته یافت نشد");

        return Result<ArticleCategoryDto>.Ok(new ArticleCategoryDto
        {
            Id = c.Id,
            Title = c.Title,
            Slug = c.Slug,
            ParentId = c.ParentId,
            SortOrder = c.SortOrder
        });
    }
}