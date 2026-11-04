using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Contact.Commands.MarkContactMessageRead;

public record MarkContactMessageReadCommand(Guid Id) : IRequest<Result>;

public class MarkContactMessageReadHandler : IRequestHandler<MarkContactMessageReadCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public MarkContactMessageReadHandler(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db; _clock = clock;
    }

    public async Task<Result> Handle(MarkContactMessageReadCommand req, CancellationToken ct)
    {
        var m = await _db.ContactMessages.FirstOrDefaultAsync(x => x.Id == req.Id, ct);
        if (m is null) return Result.Fail("پیام یافت نشد");

        m.IsRead = true;
        m.ReadAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}