using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Users.Commands.SetDefaultAddress;

public record SetDefaultAddressCommand(Guid Id) : IRequest<Result>;

public class SetDefaultAddressCommandHandler : IRequestHandler<SetDefaultAddressCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _current;

    public SetDefaultAddressCommandHandler(IApplicationDbContext db, ICurrentUserService current)
    {
        _db = db; _current = current;
    }

    public async Task<Result> Handle(SetDefaultAddressCommand req, CancellationToken ct)
    {
        if (_current.UserId is null) return Result.Fail("ابتدا وارد شوید");

        var all = await _db.Addresses
            .Where(a => a.UserId == _current.UserId.Value)
            .ToListAsync(ct);

        var target = all.FirstOrDefault(a => a.Id == req.Id);
        if (target is null) return Result.Fail("آدرس یافت نشد");

        foreach (var a in all) a.IsDefault = a.Id == req.Id;
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}