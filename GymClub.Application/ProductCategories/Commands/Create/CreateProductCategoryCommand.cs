using FluentValidation;
using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using GymClub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.ProductCategories.Commands.Create;

public record CreateProductCategoryCommand(string Title, Guid? ParentId, int SortOrder, bool IsActive)
    : IRequest<Result<Guid>>;

public class CreateProductCategoryCommandValidator : AbstractValidator<CreateProductCategoryCommand>
{
    public CreateProductCategoryCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(150);
    }
}

public class CreateProductCategoryCommandHandler : IRequestHandler<CreateProductCategoryCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    public CreateProductCategoryCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<Guid>> Handle(CreateProductCategoryCommand req, CancellationToken ct)
    {
        if (req.ParentId.HasValue &&
            !await _db.ProductCategories.AnyAsync(c => c.Id == req.ParentId && !c.IsDeleted, ct))
            return Result<Guid>.Fail("دسته والد یافت نشد");

        var slugBase = new string(req.Title.ToLowerInvariant()
            .Where(c => char.IsLetterOrDigit(c) && c < 128).ToArray());
        if (string.IsNullOrWhiteSpace(slugBase)) slugBase = "cat";
        var slug = $"{slugBase}-{Guid.NewGuid().ToString("N").Substring(0, 6)}";
        while (await _db.ProductCategories.AnyAsync(c => c.Slug == slug, ct))
            slug = $"{slugBase}-{Guid.NewGuid().ToString("N").Substring(0, 6)}";

        var e = new ProductCategory
        {
            Title = req.Title.Trim(),
            Slug = slug,
            ParentId = req.ParentId,
            SortOrder = req.SortOrder,
            IsActive = req.IsActive
        };
        _db.ProductCategories.Add(e);
        await _db.SaveChangesAsync(ct);
        return Result<Guid>.Ok(e.Id);
    }
}