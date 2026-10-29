using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Users.Commands.DeleteAddress;

public record DeleteAddressCommand(Guid Id) : IRequest<Result>;

public class DeleteAddressCommandHandler : IRequestHandler<DeleteAddressCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _current;

    public DeleteAddressCommandHandler(IApplicationDbContext db, ICurrentUserService current)
    {
        _db = db; _current = current;
    }

    public async Task<Result> Handle(DeleteAddressCommand req, CancellationToken ct)
    {
        if (_current.UserId is null) return Result.Fail("ابتدا وارد شوید");

        var entity = await _db.Addresses
            .FirstOrDefaultAsync(a => a.Id == req.Id && a.UserId == _current.UserId.Value, ct);
        if (entity is null) return Result.Fail("آدرس یافت نشد");

        var wasDefault = entity.IsDefault;
        _db.Addresses.Remove(entity);
        await _db.SaveChangesAsync(ct);

        // اگر پیش‌فرض بود، یکی دیگر را پیش‌فرض کن
        if (wasDefault)
        {
            var first = await _db.Addresses
                .Where(a => a.UserId == _current.UserId.Value)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync(ct);
            if (first is not null)
            {
                first.IsDefault = true;
                await _db.SaveChangesAsync(ct);
            }
        }

        return Result.Ok();
    }
}