using FluentValidation;
using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using GymClub.Domain.Entities;
using GymClub.Domain.Enums;
using MediatR;

namespace GymClub.Application.Articles.Commands.CreateArticle;

public record CreateArticleCommand(
    string Title,
    string Summary,
    string ContentHtml,
    string? CoverImageUrl,
    string? VideoUrl,
    ArticleType Type,
    Guid? CategoryId,
    bool Publish
) : IRequest<Result<Guid>>;

public class CreateArticleCommandValidator : AbstractValidator<CreateArticleCommand>
{
    public CreateArticleCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Summary).NotEmpty().MaximumLength(500);
        RuleFor(x => x.ContentHtml).NotEmpty();
    }
}

public class CreateArticleCommandHandler : IRequestHandler<CreateArticleCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _current;
    private readonly IDateTimeProvider _clock;

    public CreateArticleCommandHandler(IApplicationDbContext db, ICurrentUserService current, IDateTimeProvider clock)
    {
        _db = db; _current = current; _clock = clock;
    }

    public async Task<Result<Guid>> Handle(CreateArticleCommand req, CancellationToken ct)
    {
        var slug = SlugHelper.Generate(req.Title);
        // اطمینان از یکتا بودن
        var baseSlug = slug;
        var i = 1;
        while (await Task.FromResult(_db.Articles.Any(a => a.Slug == slug)))
            slug = $"{baseSlug}-{i++}";

        var entity = new Article
        {
            Title = req.Title.Trim(),
            Slug = slug,
            Summary = req.Summary.Trim(),
            ContentHtml = req.ContentHtml,
            CoverImageUrl = req.CoverImageUrl,
            VideoUrl = req.VideoUrl,
            Type = req.Type,
            CategoryId = req.CategoryId,
            AuthorId = _current.UserId!.Value,
            Status = req.Publish ? ArticleStatus.Published : ArticleStatus.Draft,
            PublishedAt = req.Publish ? _clock.UtcNow : null
        };
        _db.Articles.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Result<Guid>.Ok(entity.Id);
    }
}

internal static class SlugHelper
{
    public static string Generate(string input)
    {
        // Slug ساده بر پایه Guid برای فارسی (چون فارسی slug ندارد)
        var prefix = new string(input
            .ToLowerInvariant()
            .Where(c => char.IsLetterOrDigit(c) && c < 128)
            .Take(40)
            .ToArray());
        if (string.IsNullOrWhiteSpace(prefix)) prefix = "article";
        return $"{prefix}-{Guid.NewGuid():N}".Substring(0, Math.Min(60, prefix.Length + 1 + 8));
    }
}