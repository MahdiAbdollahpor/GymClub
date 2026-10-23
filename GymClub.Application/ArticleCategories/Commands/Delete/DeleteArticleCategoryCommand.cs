using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.ArticleCategories.Commands.Delete;

public record DeleteArticleCategoryCommand(Guid Id) : IRequest<Result>;

public class DeleteArticleCategoryCommandHandler : IRequestHandler<DeleteArticleCategoryCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public DeleteArticleCategoryCommandHandler(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db; _clock = clock;
    }

    public async Task<Result> Handle(DeleteArticleCategoryCommand req, CancellationToken ct)
    {
        var entity = await _db.ArticleCategories
            .FirstOrDefaultAsync(c => c.Id == req.Id && !c.IsDeleted, ct);
        if (entity is null) return Result.Fail("دسته یافت نشد");

        var hasArticles = await _db.Articles.AnyAsync(a => a.CategoryId == req.Id && !a.IsDeleted, ct);
        if (hasArticles) return Result.Fail("ابتدا مقالات این دسته را جابجا یا حذف کنید");

        entity.IsDeleted = true;
        entity.DeletedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}