using FluentValidation;
using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using GymClub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Articles.Commands.UpdateArticle;

public record UpdateArticleCommand(
    Guid Id,
    string Title,
    string Summary,
    string ContentHtml,
    string? CoverImageUrl,
    string? VideoUrl,
    ArticleType Type,
    Guid? CategoryId,
    bool Publish
) : IRequest<Result>;

public class UpdateArticleCommandValidator : AbstractValidator<UpdateArticleCommand>
{
    public UpdateArticleCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Summary).NotEmpty().MaximumLength(500);
        RuleFor(x => x.ContentHtml).NotEmpty();
    }
}

public class UpdateArticleCommandHandler : IRequestHandler<UpdateArticleCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public UpdateArticleCommandHandler(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db; _clock = clock;
    }

    public async Task<Result> Handle(UpdateArticleCommand req, CancellationToken ct)
    {
        var entity = await _db.Articles.FirstOrDefaultAsync(a => a.Id == req.Id && !a.IsDeleted, ct);
        if (entity is null) return Result.Fail("مقاله یافت نشد");

        entity.Title = req.Title.Trim();
        entity.Summary = req.Summary.Trim();
        entity.ContentHtml = req.ContentHtml;
        entity.CoverImageUrl = req.CoverImageUrl;
        entity.VideoUrl = req.VideoUrl;
        entity.Type = req.Type;
        entity.CategoryId = req.CategoryId;
        entity.UpdatedAt = _clock.UtcNow;

        if (req.Publish && entity.Status == ArticleStatus.Draft)
        {
            entity.Status = ArticleStatus.Published;
            entity.PublishedAt = _clock.UtcNow;
        }
        else if (!req.Publish && entity.Status == ArticleStatus.Published)
        {
            entity.Status = ArticleStatus.Draft;
        }

        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}