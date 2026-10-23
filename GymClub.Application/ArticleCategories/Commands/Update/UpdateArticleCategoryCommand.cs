using FluentValidation;
using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.ArticleCategories.Commands.Update;

public record UpdateArticleCategoryCommand(Guid Id, string Title, Guid? ParentId, int SortOrder)
    : IRequest<Result>;

public class UpdateArticleCategoryCommandValidator : AbstractValidator<UpdateArticleCategoryCommand>
{
    public UpdateArticleCategoryCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(100);
    }
}

public class UpdateArticleCategoryCommandHandler : IRequestHandler<UpdateArticleCategoryCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public UpdateArticleCategoryCommandHandler(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db; _clock = clock;
    }

    public async Task<Result> Handle(UpdateArticleCategoryCommand req, CancellationToken ct)
    {
        var entity = await _db.ArticleCategories
            .FirstOrDefaultAsync(c => c.Id == req.Id && !c.IsDeleted, ct);
        if (entity is null) return Result.Fail("دسته یافت نشد");

        if (req.ParentId == req.Id) return Result.Fail("دسته نمی‌تواند والد خودش باشد");

        entity.Title = req.Title.Trim();
        entity.ParentId = req.ParentId;
        entity.SortOrder = req.SortOrder;
        entity.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}