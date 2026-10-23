using GymClub.Application.Articles.Dtos;
using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Articles.Queries.GetArticleForEdit;

public record GetArticleForEditQuery(Guid Id) : IRequest<Result<ArticleDetailsDto>>;

public class GetArticleForEditQueryHandler : IRequestHandler<GetArticleForEditQuery, Result<ArticleDetailsDto>>
{
    private readonly IApplicationDbContext _db;

    public GetArticleForEditQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<ArticleDetailsDto>> Handle(GetArticleForEditQuery req, CancellationToken ct)
    {
        var a = await _db.Articles
            .Include(x => x.Category)
            .Include(x => x.Author)
            .FirstOrDefaultAsync(x => x.Id == req.Id && !x.IsDeleted, ct);
        if (a is null) return Result<ArticleDetailsDto>.Fail("مقاله یافت نشد");

        return Result<ArticleDetailsDto>.Ok(new ArticleDetailsDto
        {
            Id = a.Id,
            Title = a.Title,
            Slug = a.Slug,
            Summary = a.Summary,
            ContentHtml = a.ContentHtml,
            CoverImageUrl = a.CoverImageUrl,
            VideoUrl = a.VideoUrl,
            Type = a.Type,
            CategoryId = a.CategoryId,
            CategoryTitle = a.Category?.Title,
            AuthorName = a.Author.DisplayUserName,
            PublishedAt = a.PublishedAt,
            ViewCount = a.ViewCount
        });
    }
}