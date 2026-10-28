using GymClub.Application.Cart.Commands.AddToCart;
using GymClub.Application.Cart.Commands.RemoveCartItem;
using GymClub.Application.Cart.Commands.UpdateCartItem;
using GymClub.Application.Cart.Queries.GetCart;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymClub.Web.Controllers;

[Authorize]
public class CartController : Controller
{
    private readonly IMediator _m;
    public CartController(IMediator m) => _m = m;

    [HttpGet("/cart")]
    public async Task<IActionResult> Index(CancellationToken ct)
        => View(await _m.Send(new GetCartQuery(), ct));

    [HttpPost("/cart/add")]
    public async Task<IActionResult> Add(AddToCartCommand cmd, CancellationToken ct)
    {
        var r = await _m.Send(cmd, ct);
        return Json(new { success = r.Succeeded, cartCount = r.Data, message = r.Error });
    }

    [HttpPost("/cart/update")]
    public async Task<IActionResult> Update(UpdateCartItemCommand cmd, CancellationToken ct)
    {
        var r = await _m.Send(cmd, ct);
        return Json(new { success = r.Succeeded, cartCount = r.Data, message = r.Error });
    }

    [HttpPost("/cart/remove")]
    public async Task<IActionResult> Remove(RemoveCartItemCommand cmd, CancellationToken ct)
    {
        var r = await _m.Send(cmd, ct);
        return Json(new { success = r.Succeeded, cartCount = r.Data, message = r.Error });
    }
}