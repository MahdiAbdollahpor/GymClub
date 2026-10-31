using GymClub.Application.Orders.Commands.VerifyPayment;
using GymClub.Application.Payments.Queries.DetectPaymentType;
using GymClub.Application.Workouts.Commands.VerifyWorkoutPayment;
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
        // ─── تشخیص نوع پرداخت ───
        var target = await _m.Send(new DetectPaymentTypeQuery(Authority), ct);

        if (target == PaymentTarget.Order)
        {
            var r = await _m.Send(new VerifyPaymentCommand(Authority, Status), ct);
            if (!r.Succeeded)
            {
                TempData["PayError"] = r.Error;
                return RedirectToAction("Failure");
            }
            return RedirectToAction("Result", new { orderId = r.Data!.OrderId });
        }

        if (target == PaymentTarget.Workout)
        {
            var r = await _m.Send(new VerifyWorkoutPaymentCommand(Authority, Status), ct);
            if (!r.Succeeded || r.Data?.Success != true)
            {
                TempData["PayError"] = r.Error ?? r.Data?.Message ?? "پرداخت ناموفق";
                return RedirectToAction("Failure", "WorkoutPayment");
            }
            return RedirectToAction("Result", "WorkoutPayment",
                new { requestId = r.Data!.RequestId });
        }

        // ─── Authority نامشخص ───
        TempData["PayError"] = "تراکنش شناسایی نشد";
        return RedirectToAction("Failure");
    }

    [HttpGet("/payment/result")]
    public IActionResult Result(Guid orderId)
    {
        ViewBag.OrderId = orderId;
        return View();
    }

    [HttpGet("/payment/failure")]
    public IActionResult Failure() => View();
}