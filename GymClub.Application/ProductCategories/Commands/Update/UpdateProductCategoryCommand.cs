using FluentValidation;
using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.ProductCategories.Commands.Update;

public record UpdateProductCategoryCommand(Guid Id, string Title, Guid? ParentId, int SortOrder, bool IsActive)
    : IRequest<Result>;

public class UpdateProductCategoryCommandValidator : AbstractValidator<UpdateProductCategoryCommand>
{
    public UpdateProductCategoryCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(150);
    }
}

public class UpdateProductCategoryCommandHandler : IRequestHandler<UpdateProductCategoryCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;
    public UpdateProductCategoryCommandHandler(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db; _clock = clock;
    }

    public async Task<Result> Handle(UpdateProductCategoryCommand req, CancellationToken ct)
    {
        var e = await _db.ProductCategories.FirstOrDefaultAsync(c => c.Id == req.Id && !c.IsDeleted, ct);
        if (e is null) return Result.Fail("دسته یافت نشد");
        if (req.ParentId == req.Id) return Result.Fail("دسته نمی‌تواند والد خودش باشد");

        e.Title = req.Title.Trim();
        e.ParentId = req.ParentId;
        e.SortOrder = req.SortOrder;
        e.IsActive = req.IsActive;
        e.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}