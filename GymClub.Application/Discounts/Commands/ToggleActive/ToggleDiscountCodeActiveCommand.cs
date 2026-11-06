using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Discounts.Commands.ToggleActive;

public record ToggleDiscountCodeActiveCommand(Guid Id) : IRequest<Result<bool>>;

public class ToggleDiscountCodeActiveHandler
    : IRequestHandler<ToggleDiscountCodeActiveCommand, Result<bool>>
{
    private readonly IApplicationDbContext _db;
    public ToggleDiscountCodeActiveHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<bool>> Handle(ToggleDiscountCodeActiveCommand req, CancellationToken ct)
    {
        var d = await _db.DiscountCodes.FirstOrDefaultAsync(x => x.Id == req.Id && !x.IsDeleted, ct);
        if (d is null) return Result<bool>.Fail("کد تخفیف یافت نشد");

        d.IsActive = !d.IsActive;
        await _db.SaveChangesAsync(ct);
        return Result<bool>.Ok(d.IsActive);
    }
}