using FluentValidation;
using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Cart.Commands.UpdateCartItem;

public record UpdateCartItemCommand(Guid CartItemId, int Quantity) : IRequest<Result<int>>;

public class UpdateCartItemCommandValidator : AbstractValidator<UpdateCartItemCommand>
{
    public UpdateCartItemCommandValidator()
    {
        RuleFor(x => x.CartItemId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThanOrEqualTo(0);
    }
}

public class UpdateCartItemCommandHandler : IRequestHandler<UpdateCartItemCommand, Result<int>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _current;

    public UpdateCartItemCommandHandler(IApplicationDbContext db, ICurrentUserService current)
    {
        _db = db; _current = current;
    }

    public async Task<Result<int>> Handle(UpdateCartItemCommand req, CancellationToken ct)
    {
        if (_current.UserId is null) return Result<int>.Fail("ابتدا وارد شوید");

        var item = await _db.CartItems
            .Include(i => i.Cart)
            .Include(i => i.Product)
            .FirstOrDefaultAsync(i => i.Id == req.CartItemId && i.Cart.UserId == _current.UserId.Value, ct);
        if (item is null) return Result<int>.Fail("آیتم یافت نشد");

        if (req.Quantity <= 0)
        {
            _db.CartItems.Remove(item);
        }
        else
        {
            if (req.Quantity > item.Product.Stock) return Result<int>.Fail("موجودی کافی نیست");
            item.Quantity = req.Quantity;
            item.UnitPriceSnapshot = item.Product.DiscountPrice ?? item.Product.Price;
        }

        await _db.SaveChangesAsync(ct);
        var count = await _db.CartItems
            .Where(i => i.CartId == item.CartId)
            .SumAsync(i => i.Quantity, ct);
        return Result<int>.Ok(count);
    }
}