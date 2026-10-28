using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Cart.Commands.RemoveCartItem;

public record RemoveCartItemCommand(Guid CartItemId) : IRequest<Result<int>>;

public class RemoveCartItemCommandHandler : IRequestHandler<RemoveCartItemCommand, Result<int>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _current;

    public RemoveCartItemCommandHandler(IApplicationDbContext db, ICurrentUserService current)
    {
        _db = db; _current = current;
    }

    public async Task<Result<int>> Handle(RemoveCartItemCommand req, CancellationToken ct)
    {
        if (_current.UserId is null) return Result<int>.Fail("ابتدا وارد شوید");

        var item = await _db.CartItems
            .Include(i => i.Cart)
            .FirstOrDefaultAsync(i => i.Id == req.CartItemId && i.Cart.UserId == _current.UserId.Value, ct);
        if (item is null) return Result<int>.Fail("آیتم یافت نشد");

        var cartId = item.CartId;
        _db.CartItems.Remove(item);
        await _db.SaveChangesAsync(ct);

        var count = await _db.CartItems.Where(i => i.CartId == cartId).SumAsync(i => i.Quantity, ct);
        return Result<int>.Ok(count);
    }
}