using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Products.Commands.DeleteProduct;

public record DeleteProductCommand(Guid Id) : IRequest<Result>;

public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly IImageStorageService _images;
    private readonly IDateTimeProvider _clock;

    public DeleteProductCommandHandler(IApplicationDbContext db, IImageStorageService images, IDateTimeProvider clock)
    {
        _db = db; _images = images; _clock = clock;
    }

    public async Task<Result> Handle(DeleteProductCommand req, CancellationToken ct)
    {
        var p = await _db.Products
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == req.Id && !x.IsDeleted, ct);
        if (p is null) return Result.Fail("محصول یافت نشد");

        foreach (var img in p.Images)
            await _images.DeleteAsync(img.Url, ct);

        p.IsDeleted = true;
        p.DeletedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}