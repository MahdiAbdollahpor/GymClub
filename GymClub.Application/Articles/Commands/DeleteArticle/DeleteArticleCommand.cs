using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Articles.Commands.DeleteArticle;

public record DeleteArticleCommand(Guid Id) : IRequest<Result>;

public class DeleteArticleCommandHandler : IRequestHandler<DeleteArticleCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public DeleteArticleCommandHandler(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db; _clock = clock;
    }

    public async Task<Result> Handle(DeleteArticleCommand req, CancellationToken ct)
    {
        var entity = await _db.Articles.FirstOrDefaultAsync(a => a.Id == req.Id && !a.IsDeleted, ct);
        if (entity is null) return Result.Fail("مقاله یافت نشد");

        entity.IsDeleted = true;
        entity.DeletedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}