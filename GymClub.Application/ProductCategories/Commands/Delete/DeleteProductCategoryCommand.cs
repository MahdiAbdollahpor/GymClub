using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.ProductCategories.Commands.Delete;

public record DeleteProductCategoryCommand(Guid Id) : IRequest<Result>;

public class DeleteProductCategoryCommandHandler : IRequestHandler<DeleteProductCategoryCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;
    public DeleteProductCategoryCommandHandler(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db; _clock = clock;
    }

    public async Task<Result> Handle(DeleteProductCategoryCommand req, CancellationToken ct)
    {
        var e = await _db.ProductCategories.FirstOrDefaultAsync(c => c.Id == req.Id && !c.IsDeleted, ct);
        if (e is null) return Result.Fail("دسته یافت نشد");

        var hasProducts = await _db.Products.AnyAsync(p => p.CategoryId == req.Id && !p.IsDeleted, ct);
        if (hasProducts) return Result.Fail("ابتدا محصولات این دسته را جابجا یا حذف کنید");

        e.IsDeleted = true;
        e.DeletedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}