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
    ArticleType Type,
    Guid? CategoryId,
    bool Publish,
    Stream? CoverImageStream = null,
    string? CoverImageFileName = null,
    string? CoverImageContentType = null,
    Stream? VideoStream = null,
    string? VideoFileName = null,
    string? VideoContentType = null,
    bool RemoveCoverImage = false,
    bool RemoveVideo = false
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
    private readonly IImageStorageService _images;
    private readonly IVideoStorageService _videos;
    private readonly IDateTimeProvider _clock;

    public UpdateArticleCommandHandler(
        IApplicationDbContext db,
        IImageStorageService images,
        IVideoStorageService videos,
        IDateTimeProvider clock)
    {
        _db = db; _images = images; _videos = videos; _clock = clock;
    }

    public async Task<Result> Handle(UpdateArticleCommand req, CancellationToken ct)
    {
        var entity = await _db.Articles
            .FirstOrDefaultAsync(a => a.Id == req.Id && !a.IsDeleted, ct);
        if (entity is null) return Result.Fail("مقاله یافت نشد");

        var oldCover = entity.CoverImageUrl;
        var oldVideo = entity.VideoUrl;

        try
        {
            // عکس
            if (req.CoverImageStream is not null && !string.IsNullOrWhiteSpace(req.CoverImageFileName))
            {
                entity.CoverImageUrl = await _images.UploadAsync(
                    req.CoverImageStream, req.CoverImageFileName,
                    req.CoverImageContentType ?? "image/jpeg", ct);
                if (!string.IsNullOrWhiteSpace(oldCover))
                    await _images.DeleteAsync(oldCover, ct);
            }
            else if (req.RemoveCoverImage && !string.IsNullOrWhiteSpace(oldCover))
            {
                await _images.DeleteAsync(oldCover, ct);
                entity.CoverImageUrl = null;
            }

            // ویدیو
            if (req.VideoStream is not null && !string.IsNullOrWhiteSpace(req.VideoFileName))
            {
                entity.VideoUrl = await _videos.UploadAsync(
                    req.VideoStream, req.VideoFileName,
                    req.VideoContentType ?? "video/mp4", ct);
                if (!string.IsNullOrWhiteSpace(oldVideo))
                    await _videos.DeleteAsync(oldVideo, ct);
            }
            else if (req.RemoveVideo && !string.IsNullOrWhiteSpace(oldVideo))
            {
                await _videos.DeleteAsync(oldVideo, ct);
                entity.VideoUrl = null;
            }
        }
        catch (Exception ex)
        {
            return Result.Fail($"خطا در آپلود فایل: {ex.Message}");
        }

        entity.Title = req.Title.Trim();
        entity.Summary = req.Summary.Trim();
        entity.ContentHtml = req.ContentHtml;
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