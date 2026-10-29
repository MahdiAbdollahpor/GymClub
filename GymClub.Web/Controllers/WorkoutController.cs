using GymClub.Application.Workouts.Commands.CreateWorkoutRequest;
using GymClub.Application.Workouts.Commands.SubmitAnswers;
using GymClub.Application.Workouts.Queries.GetActiveQuestionnaire;
using GymClub.Application.Workouts.Queries.GetMyWorkoutRequest;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymClub.Web.Controllers;

public class WorkoutController : Controller
{
    private readonly IMediator _m;
    public WorkoutController(IMediator m) => _m = m;

    [HttpGet("/workout")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var q = await _m.Send(new GetActiveQuestionnaireQuery(), ct);
        ViewBag.Questionnaire = q.Data;

        if (User.Identity?.IsAuthenticated == true)
        {
            var my = await _m.Send(new GetMyWorkoutRequestQuery(), ct);
            ViewBag.MyRequest = my.Data;
        }
        return View();
    }

    [HttpPost("/workout/request")]
    [Authorize]
    public async Task<IActionResult> CreateRequest(CancellationToken ct)
    {
        var r = await _m.Send(new CreateWorkoutRequestCommand(), ct);
        if (!r.Succeeded) return Json(new { success = false, message = r.Error });
        return Json(new { success = true, paymentUrl = r.Data!.PaymentUrl });
    }

    [HttpGet("/workout/answers")]
    [Authorize]
    public async Task<IActionResult> Answers(CancellationToken ct)
    {
        var my = await _m.Send(new GetMyWorkoutRequestQuery(), ct);
        var q = await _m.Send(new GetActiveQuestionnaireQuery(), ct);

        if (!q.Succeeded) return NotFound();
        if (my.Succeeded && my.Data?.Status is not Domain.Enums.WorkoutRequestStatus.Paid)
        {
            TempData["Error"] = "برای پر کردن پرسشنامه ابتدا باید هزینه را پرداخت کنید.";
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Questionnaire = q.Data;
        return View();
    }

    [HttpPost("/workout/answers")]
    [Authorize]
    public async Task<IActionResult> Answers([FromForm] List<AnswerInput> answers, CancellationToken ct)
    {
        var r = await _m.Send(new SubmitWorkoutAnswersCommand(answers), ct);
        if (!r.Succeeded) return Json(new { success = false, message = r.Error });
        return Json(new { success = true });
    }
}