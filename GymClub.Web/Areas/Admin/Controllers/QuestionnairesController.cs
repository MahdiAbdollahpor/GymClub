using GymClub.Application.Workouts.Commands.SaveQuestionnaire;
using GymClub.Application.Workouts.Queries.GetAllQuestionnaires;
using GymClub.Application.Workouts.Queries.GetQuestionnaireForEdit;
using GymClub.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymClub.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
[Route("admin/questionnaires")]
public class QuestionnairesController : Controller
{
    private readonly IMediator _m;
    public QuestionnairesController(IMediator m) => _m = m;

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
        => View(await _m.Send(new GetAllQuestionnairesQuery(), ct));

    [HttpGet("create")]
    public IActionResult Create() => View(new SaveQuestionnaireCommand(null, "", "", 0, true, new()));

    [HttpPost("create")]
    public async Task<IActionResult> Create(SaveQuestionnaireCommand cmd, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(cmd);
        var r = await _m.Send(cmd, ct);
        if (!r.Succeeded) { ModelState.AddModelError("", r.Error!); return View(cmd); }
        TempData["Success"] = "پرسشنامه ایجاد شد";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("edit/{id:guid}")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var r = await _m.Send(new GetQuestionnaireForEditQuery(id), ct);
        if (!r.Succeeded) return NotFound();
        var d = r.Data!;

        var cmd = new SaveQuestionnaireCommand(
            d.Id, d.Title, d.Description, d.Price, d.IsActive,
            d.Questions.Select(q => new QuestionInput
            {
                Id = q.Id,
                QuestionText = q.QuestionText,
                Type = q.Type,
                Options = q.Options,
                IsRequired = q.IsRequired,
                GroupName = q.GroupName,
                SortOrder = q.SortOrder
            }).ToList());

        return View(cmd);
    }

    [HttpPost("edit/{id:guid}")]
    public async Task<IActionResult> Edit(Guid id, SaveQuestionnaireCommand cmd, CancellationToken ct)
    {
        if (cmd.Id != id) return BadRequest();
        if (!ModelState.IsValid) return View(cmd);
        var r = await _m.Send(cmd, ct);
        if (!r.Succeeded) { ModelState.AddModelError("", r.Error!); return View(cmd); }
        TempData["Success"] = "پرسشنامه به‌روزرسانی شد";
        return RedirectToAction(nameof(Index));
    }
}