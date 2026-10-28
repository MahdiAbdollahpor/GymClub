using FluentValidation;
using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using GymClub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Cart.Commands.AddToCart;

public record AddToCartCommand(Guid ProductId, int Quantity) : IRequest<Result<int>>;

public class AddToCartCommandValidator : AbstractValidator<AddToCartCommand>
{
    public AddToCartCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}

public class AddToCartCommandHandler : IRequestHandler<AddToCartCommand, Result<int>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _current;

    public AddToCartCommandHandler(IApplicationDbContext db, ICurrentUserService current)
    {
        _db = db; _current = current;
    }

    public async Task<Result<int>> Handle(AddToCartCommand req, CancellationToken ct)
    {
        if (_current.UserId is null) return Result<int>.Fail("ابتدا وارد شوید");

        var product = await _db.Products
            .FirstOrDefaultAsync(p => p.Id == req.ProductId && !p.IsDeleted && p.IsActive, ct);
        if (product is null) return Result<int>.Fail("محصول یافت نشد");
        if (product.Stock < req.Quantity) return Result<int>.Fail("موجودی کافی نیست");

        var cart = await _db.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == _current.UserId.Value, ct);

        if (cart is null)
        {
            cart = new Domain.Entities.Cart { UserId = _current.UserId.Value };
            _db.Carts.Add(cart);
            await _db.SaveChangesAsync(ct);
        }

        var price = product.DiscountPrice ?? product.Price;
        var existing = cart.Items.FirstOrDefault(i => i.ProductId == req.ProductId);

        if (existing is not null)
        {
            var newQty = existing.Quantity + req.Quantity;
            if (newQty > product.Stock) return Result<int>.Fail("موجودی کافی نیست");
            existing.Quantity = newQty;
            existing.UnitPriceSnapshot = price;   // قیمت را با جدیدترین همگام کن
        }
        else
        {
            _db.CartItems.Add(new CartItem
            {
                CartId = cart.Id,
                ProductId = req.ProductId,
                Quantity = req.Quantity,
                UnitPriceSnapshot = price
            });
        }

        await _db.SaveChangesAsync(ct);

        var count = await _db.CartItems
            .Where(i => i.CartId == cart.Id)
            .SumAsync(i => i.Quantity, ct);

        return Result<int>.Ok(count);
    }
}