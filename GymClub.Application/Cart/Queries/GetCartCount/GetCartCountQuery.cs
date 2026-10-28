using GymClub.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Cart.Queries.GetCartCount;

public record GetCartCountQuery : IRequest<int>;

public class GetCartCountQueryHandler : IRequestHandler<GetCartCountQuery, int>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _current;

    public GetCartCountQueryHandler(IApplicationDbContext db, ICurrentUserService current)
    {
        _db = db; _current = current;
    }

    public async Task<int> Handle(GetCartCountQuery req, CancellationToken ct)
    {
        if (_current.UserId is null) return 0;
        var cartId = await _db.Carts
            .Where(c => c.UserId == _current.UserId.Value)
            .Select(c => (Guid?)c.Id).FirstOrDefaultAsync(ct);
        if (cartId is null) return 0;
        return await _db.CartItems.Where(i => i.CartId == cartId.Value).SumAsync(i => i.Quantity, ct);
    }
}