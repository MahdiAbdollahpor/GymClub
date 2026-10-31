using GymClub.Application.Workouts.Commands.SaveWorkoutProgram;
using GymClub.Application.Workouts.Queries.GetAllWorkoutRequests;
using GymClub.Application.Workouts.Queries.GetSavedPrograms;
using GymClub.Application.Workouts.Queries.GetWorkoutRequestDetails;
using GymClub.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymClub.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
[Route("admin/workout-requests")]
public class WorkoutRequestsController : Controller
{
    private readonly IMediator _m;
    public WorkoutRequestsController(IMediator m) => _m = m;

    [HttpGet("")]
    public async Task<IActionResult> Index(string? search, WorkoutRequestStatus? status, int page = 1, CancellationToken ct = default)
    {
        var list = await _m.Send(new GetAllWorkoutRequestsQuery(search, status, page), ct);
        ViewBag.Search = search;
        ViewBag.Status = status;
        return View(list);
    }

    [HttpGet("details/{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var r = await _m.Send(new GetWorkoutRequestDetailsQuery(id), ct);
        if (!r.Succeeded) return NotFound();
        return View(r.Data);
    }


    [HttpGet("programs")]
    public async Task<IActionResult> Programs(string? search, int page = 1, CancellationToken ct = default)
    {
        var list = await _m.Send(new GetSavedProgramsQuery(search, page), ct);
        ViewBag.Search = search;
        return View(list);
    }

    [HttpPost("save-program/{id:guid}")]
    public async Task<IActionResult> SaveProgram(Guid id, [FromBody] SaveWorkoutProgramCommand cmd, CancellationToken ct)
    {
        Console.WriteLine($"[SaveProgram] id={id}, cmd.RequestId={cmd.RequestId}, days={cmd.Days?.Count}");

        if (id != cmd.RequestId) return BadRequest(new { message = "RequestId mismatch" });

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return BadRequest(new { message = string.Join(" | ", errors) });
        }

        var r = await _m.Send(cmd, ct);
        Console.WriteLine($"[SaveProgram] result: {r.Succeeded}, error: {r.Error}");

        return Json(new { success = r.Succeeded, message = r.Error, programId = r.Data });
    }
}