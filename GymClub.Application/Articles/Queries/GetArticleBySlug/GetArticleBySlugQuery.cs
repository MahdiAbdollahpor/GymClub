using GymClub.Application.Articles.Dtos;
using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using GymClub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Articles.Queries.GetArticleBySlug;

public record GetArticleBySlugQuery(string Slug, bool IncrementView = true)
    : IRequest<Result<ArticleDetailsDto>>;

public class GetArticleBySlugQueryHandler
    : IRequestHandler<GetArticleBySlugQuery, Result<ArticleDetailsDto>>
{
    private readonly IApplicationDbContext _db;

    public GetArticleBySlugQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<ArticleDetailsDto>> Handle(GetArticleBySlugQuery req, CancellationToken ct)
    {
        var article = await _db.Articles
            .Include(a => a.Category)
            .Include(a => a.Author)
            .FirstOrDefaultAsync(a => a.Slug == req.Slug
                                   && !a.IsDeleted
                                   && a.Status == ArticleStatus.Published, ct);

        if (article is null) return Result<ArticleDetailsDto>.Fail("مقاله یافت نشد");

        if (req.IncrementView)
        {
            article.ViewCount++;
            await _db.SaveChangesAsync(ct);
        }

        return Result<ArticleDetailsDto>.Ok(new ArticleDetailsDto
        {
            Id = article.Id,
            Title = article.Title,
            Slug = article.Slug,
            Summary = article.Summary,
            ContentHtml = article.ContentHtml,
            CoverImageUrl = article.CoverImageUrl,
            VideoUrl = article.VideoUrl,
            Type = article.Type,
            CategoryId = article.CategoryId,
            CategoryTitle = article.Category?.Title,
            AuthorName = article.Author.DisplayUserName,
            PublishedAt = article.PublishedAt,
            ViewCount = article.ViewCount
        });
    }
}