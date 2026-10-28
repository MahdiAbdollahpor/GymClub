using GymClub.Application.Cart.Dtos;
using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Cart.Queries.GetCart;

public record GetCartQuery : IRequest<CartDto>;

public class GetCartQueryHandler : IRequestHandler<GetCartQuery, CartDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _current;
    private readonly IImageStorageService _images;

    public GetCartQueryHandler(IApplicationDbContext db, ICurrentUserService current, IImageStorageService images)
    {
        _db = db; _current = current; _images = images;
    }

    public async Task<CartDto> Handle(GetCartQuery req, CancellationToken ct)
    {
        if (_current.UserId is null) return new CartDto();

        var cart = await _db.Carts
            .AsNoTracking()
            .Where(c => c.UserId == _current.UserId.Value)
            .Select(c => new
            {
                Items = c.Items.Select(i => new
                {
                    i.Id,
                    i.ProductId,
                    i.Quantity,
                    i.UnitPriceSnapshot,
                    ProductName = i.Product.Name,
                    ProductSlug = i.Product.Slug,
                    Stock = i.Product.Stock,
                    MainImage = i.Product.Images.Where(im => im.IsMain).Select(im => im.Url).FirstOrDefault()
                }).ToList()
            })
            .FirstOrDefaultAsync(ct);

        if (cart is null) return new CartDto();

        return new CartDto
        {
            Items = cart.Items.Select(i => new CartItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                ProductSlug = i.ProductSlug,
                ProductImageUrl = string.IsNullOrEmpty(i.MainImage) ? null : _images.GetPublicUrl(i.MainImage),
                Quantity = i.Quantity,
                StockAvailable = i.Stock,
                UnitPrice = i.UnitPriceSnapshot
            }).ToList()
        };
    }
}