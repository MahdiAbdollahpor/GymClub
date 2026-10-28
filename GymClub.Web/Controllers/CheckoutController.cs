using GymClub.Application.Common.Interfaces;
using GymClub.Application.Discounts.Queries.ValidateDiscountCode;
using GymClub.Application.Orders.Commands.Checkout;
using GymClub.Application.Users.Queries.GetAddresses;   // فرض: از گام قبل داری
using GymClub.Application.Cart.Queries.GetCart;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymClub.Web.Controllers;

[Authorize]
public class CheckoutController : Controller
{
    private readonly IMediator _m;
    public CheckoutController(IMediator m) => _m = m;

    [HttpGet("/checkout")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var cart = await _m.Send(new GetCartQuery(), ct);
        if (!cart.Items.Any()) return RedirectToAction("Index", "Cart");

        var addresses = await _m.Send(new GetAddressesQuery(), ct);
        ViewBag.Cart = cart;
        ViewBag.Addresses = addresses;
        return View();
    }

    [HttpPost("/checkout/validate-discount")]
    public async Task<IActionResult> ValidateDiscount(string code, decimal subTotal, CancellationToken ct)
    {
        var r = await _m.Send(new ValidateDiscountCodeQuery(code, subTotal), ct);
        return Json(new
        {
            success = r.Succeeded,
            message = r.Error,
            amount = r.Data?.DiscountAmount ?? 0,
            description = r.Data?.Description
        });
    }

    [HttpPost("/checkout/place-order")]
    public async Task<IActionResult> PlaceOrder(CheckoutCommand cmd, CancellationToken ct)
    {
        var r = await _m.Send(cmd, ct);
        if (!r.Succeeded) return Json(new { success = false, message = r.Error });
        return Json(new { success = true, paymentUrl = r.Data!.PaymentUrl, orderNumber = r.Data.OrderNumber });
    }
}