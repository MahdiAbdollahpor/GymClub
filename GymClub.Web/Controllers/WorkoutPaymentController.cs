using GymClub.Application.Workouts.Commands.VerifyWorkoutPayment;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymClub.Web.Controllers;

[Authorize]
public class WorkoutPaymentController : Controller
{
    private readonly IMediator _m;
    public WorkoutPaymentController(IMediator m) => _m = m;

    [AllowAnonymous]
    [HttpGet("/workout/payment/callback")]
    public async Task<IActionResult> Callback(string Authority, string Status, CancellationToken ct)
    {
        var r = await _m.Send(new VerifyWorkoutPaymentCommand(Authority, Status), ct);
        if (!r.Succeeded || r.Data?.Success != true)
        {
            TempData["PayError"] = r.Error ?? r.Data?.Message ?? "پرداخت ناموفق";
            return RedirectToAction("Failure", "WorkoutPayment");
        }
        return RedirectToAction("Result", new { requestId = r.Data!.RequestId });
    }

    [HttpGet("/workout/payment/result")]
    public IActionResult Result(Guid requestId)
    {
        ViewBag.RequestId = requestId;
        return View();
    }

    [HttpGet("/workout/payment/failure")]
    public IActionResult Failure() => View();
}