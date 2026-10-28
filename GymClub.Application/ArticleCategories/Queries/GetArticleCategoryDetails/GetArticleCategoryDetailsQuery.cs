using GymClub.Application.Articles.Dtos;
using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using GymClub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.ArticleCategories.Queries.GetArticleCategoryDetails;

public class ArticleCategoryDetailsDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public string? ParentTitle { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<ArticleCardDto> Articles { get; set; } = new();
}

public record GetArticleCategoryDetailsQuery(Guid Id) : IRequest<Result<ArticleCategoryDetailsDto>>;

public class GetArticleCategoryDetailsQueryHandler
    : IRequestHandler<GetArticleCategoryDetailsQuery, Result<ArticleCategoryDetailsDto>>
{
    private readonly IApplicationDbContext _db;
    public GetArticleCategoryDetailsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<ArticleCategoryDetailsDto>> Handle(
        GetArticleCategoryDetailsQuery req, CancellationToken ct)
    {
        var c = await _db.ArticleCategories
            .AsNoTracking()
            .Include(x => x.Parent)
            .Include(x => x.Articles)
            .FirstOrDefaultAsync(x => x.Id == req.Id && !x.IsDeleted, ct);

        if (c is null) return Result<ArticleCategoryDetailsDto>.Fail("دسته یافت نشد");

        return Result<ArticleCategoryDetailsDto>.Ok(new ArticleCategoryDetailsDto
        {
            Id = c.Id,
            Title = c.Title,
            Slug = c.Slug,
            ParentId = c.ParentId,
            ParentTitle = c.Parent?.Title,
            SortOrder = c.SortOrder,
            CreatedAt = c.CreatedAt,
            Articles = c.Articles
                .Where(a => !a.IsDeleted)
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new ArticleCardDto
                {
                    Id = a.Id,
                    Title = a.Title,
                    Slug = a.Slug,
                    Summary = a.Summary,
                    CoverImageUrl = a.CoverImageUrl,
                    Type = a.Type,
                    CategoryTitle = c.Title,
                    PublishedAt = a.PublishedAt,
                    ViewCount = a.ViewCount,
                    HasVideo = a.VideoUrl != null
                }).ToList()
        });
    }
}