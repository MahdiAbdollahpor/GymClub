using GymClub.Application.Orders.Commands.VerifyPayment;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GymClub.Web.Controllers;

public class PaymentController : Controller
{
    private readonly IMediator _m;
    public PaymentController(IMediator m) => _m = m;

    [HttpGet("/payment/callback")]
    public async Task<IActionResult> Callback(string Authority, string Status, CancellationToken ct)
    {
        var r = await _m.Send(new VerifyPaymentCommand(Authority, Status), ct);
        if (!r.Succeeded)
        {
            TempData["PayError"] = r.Error;
            return RedirectToAction("Failure");
        }
        return RedirectToAction("Result", new { orderId = r.Data!.OrderId });
    }

    [HttpGet("/payment/result")]
    public async Task<IActionResult> Result(Guid orderId, CancellationToken ct)
    {
        ViewBag.OrderId = orderId;
        return View();
    }

    [HttpGet("/payment/failure")]
    public IActionResult Failure() => View();
}