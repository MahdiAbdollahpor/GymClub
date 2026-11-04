using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Contact.Commands.DeleteContactMessage;

public record DeleteContactMessageCommand(Guid Id) : IRequest<Result>;

public class DeleteContactMessageHandler : IRequestHandler<DeleteContactMessageCommand, Result>
{
    private readonly IApplicationDbContext _db;
    public DeleteContactMessageHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result> Handle(DeleteContactMessageCommand req, CancellationToken ct)
    {
        var m = await _db.ContactMessages.FirstOrDefaultAsync(x => x.Id == req.Id, ct);
        if (m is null) return Result.Fail("پیام یافت نشد");

        _db.ContactMessages.Remove(m);
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}