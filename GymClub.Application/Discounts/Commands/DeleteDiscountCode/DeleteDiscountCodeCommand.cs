using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Discounts.Commands.DeleteDiscountCode;

public record DeleteDiscountCodeCommand(Guid Id) : IRequest<Result>;

public class DeleteDiscountCodeHandler : IRequestHandler<DeleteDiscountCodeCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;
    public DeleteDiscountCodeHandler(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db; _clock = clock;
    }

    public async Task<Result> Handle(DeleteDiscountCodeCommand req, CancellationToken ct)
    {
        var entity = await _db.DiscountCodes.FirstOrDefaultAsync(d => d.Id == req.Id && !d.IsDeleted, ct);
        if (entity is null) return Result.Fail("کد تخفیف یافت نشد");

        entity.IsDeleted = true;
        entity.DeletedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}