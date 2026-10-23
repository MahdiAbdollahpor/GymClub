using FluentValidation;
using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using GymClub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.ArticleCategories.Commands.Create;

public record CreateArticleCategoryCommand(string Title, Guid? ParentId, int SortOrder)
    : IRequest<Result<Guid>>;

public class CreateArticleCategoryCommandValidator : AbstractValidator<CreateArticleCategoryCommand>
{
    public CreateArticleCategoryCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(100);
    }
}

public class CreateArticleCategoryCommandHandler : IRequestHandler<CreateArticleCategoryCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;

    public CreateArticleCategoryCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<Guid>> Handle(CreateArticleCategoryCommand req, CancellationToken ct)
    {
        if (req.ParentId.HasValue &&
            !await _db.ArticleCategories.AnyAsync(c => c.Id == req.ParentId && !c.IsDeleted, ct))
            return Result<Guid>.Fail("دسته والد یافت نشد");

        var slugBase = new string(req.Title
            .ToLowerInvariant()
            .Where(c => char.IsLetterOrDigit(c) && c < 128)
            .ToArray());
        if (string.IsNullOrWhiteSpace(slugBase)) slugBase = "cat";
        var slug = $"{slugBase}-{Guid.NewGuid().ToString("N").Substring(0, 6)}";
        while (await _db.ArticleCategories.AnyAsync(c => c.Slug == slug, ct))
            slug = $"{slugBase}-{Guid.NewGuid().ToString("N").Substring(0, 6)}";

        var entity = new ArticleCategory
        {
            Title = req.Title.Trim(),
            Slug = slug,
            ParentId = req.ParentId,
            SortOrder = req.SortOrder
        };
        _db.ArticleCategories.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Result<Guid>.Ok(entity.Id);
    }
}