using FluentValidation;
using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using GymClub.Domain.Entities;
using GymClub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Articles.Commands.CreateArticle;

public record CreateArticleCommand(
    string Title,
    string Summary,
    string ContentHtml,
    ArticleType Type,
    Guid? CategoryId,
    bool Publish,
    // فایل‌های آپلود
    Stream? CoverImageStream = null,
    string? CoverImageFileName = null,
    string? CoverImageContentType = null,
    Stream? VideoStream = null,
    string? VideoFileName = null,
    string? VideoContentType = null
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
    private readonly IImageStorageService _images;
    private readonly IVideoStorageService _videos;
    private readonly ICurrentUserService _current;
    private readonly IDateTimeProvider _clock;

    public CreateArticleCommandHandler(
        IApplicationDbContext db,
        IImageStorageService images,
        IVideoStorageService videos,
        ICurrentUserService current,
        IDateTimeProvider clock)
    {
        _db = db; _images = images; _videos = videos; _current = current; _clock = clock;
    }

    public async Task<Result<Guid>> Handle(CreateArticleCommand req, CancellationToken ct)
    {
        string? coverUrl = null;
        string? videoUrl = null;

        try
        {
            if (req.CoverImageStream is not null && !string.IsNullOrWhiteSpace(req.CoverImageFileName))
            {
                coverUrl = await _images.UploadAsync(
                    req.CoverImageStream,
                    req.CoverImageFileName,
                    req.CoverImageContentType ?? "image/jpeg",
                    ct);
            }

            if (req.VideoStream is not null && !string.IsNullOrWhiteSpace(req.VideoFileName))
            {
                videoUrl = await _videos.UploadAsync(
                    req.VideoStream,
                    req.VideoFileName,
                    req.VideoContentType ?? "video/mp4",
                    ct);
            }
        }
        catch (Exception ex)
        {
            return Result<Guid>.Fail($"خطا در آپلود فایل: {ex.Message}");
        }

        // slug
        var slugBase = new string(req.Title.ToLowerInvariant()
            .Where(c => char.IsLetterOrDigit(c) && c < 128).Take(40).ToArray());
        if (string.IsNullOrWhiteSpace(slugBase)) slugBase = "article";
        var slug = $"{slugBase}-{Guid.NewGuid():N}".Substring(0, Math.Min(60, slugBase.Length + 1 + 8));
        while (await _db.Articles.AnyAsync(a => a.Slug == slug, ct))
            slug = $"{slugBase}-{Guid.NewGuid().ToString("N").Substring(0, 8)}";

        var entity = new Article
        {
            Title = req.Title.Trim(),
            Slug = slug,
            Summary = req.Summary.Trim(),
            ContentHtml = req.ContentHtml,
            CoverImageUrl = coverUrl,
            VideoUrl = videoUrl,
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