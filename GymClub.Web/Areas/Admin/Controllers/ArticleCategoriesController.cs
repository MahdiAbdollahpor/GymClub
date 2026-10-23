using GymClub.Application.ArticleCategories.Commands.Create;
using GymClub.Application.ArticleCategories.Commands.Delete;
using GymClub.Application.ArticleCategories.Commands.Update;
using GymClub.Application.ArticleCategories.Queries.GetForEdit;
using GymClub.Application.Articles.Queries.GetArticleCategories;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymClub.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
[Route("admin/article-categories")]
public class ArticleCategoriesController : Controller
{
    private readonly IMediator _mediator;
    public ArticleCategoriesController(IMediator mediator) => _mediator = mediator;

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var list = await _mediator.Send(new GetArticleCategoriesQuery(), ct);
        return View(list);
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        ViewBag.Categories = await _mediator.Send(new GetArticleCategoriesQuery(), ct);
        return View(new CreateArticleCategoryCommand("", null, 0));
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create(CreateArticleCategoryCommand cmd, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Categories = await _mediator.Send(new GetArticleCategoriesQuery(), ct);
            return View(cmd);
        }
        var r = await _mediator.Send(cmd, ct);
        if (!r.Succeeded)
        {
            ModelState.AddModelError("", r.Error!);
            ViewBag.Categories = await _mediator.Send(new GetArticleCategoriesQuery(), ct);
            return View(cmd);
        }
        TempData["Success"] = "دسته ایجاد شد";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("edit/{id:guid}")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var r = await _mediator.Send(new GetArticleCategoryForEditQuery(id), ct);
        if (!r.Succeeded) return NotFound();

        ViewBag.Categories = await _mediator.Send(new GetArticleCategoriesQuery(), ct);
        var d = r.Data!;
        return View(new UpdateArticleCategoryCommand(d.Id, d.Title, d.ParentId, d.SortOrder));
    }

    [HttpPost("edit/{id:guid}")]
    public async Task<IActionResult> Edit(Guid id, UpdateArticleCategoryCommand cmd, CancellationToken ct)
    {
        if (id != cmd.Id) return BadRequest();
        if (!ModelState.IsValid)
        {
            ViewBag.Categories = await _mediator.Send(new GetArticleCategoriesQuery(), ct);
            return View(cmd);
        }
        var r = await _mediator.Send(cmd, ct);
        if (!r.Succeeded)
        {
            ModelState.AddModelError("", r.Error!);
            ViewBag.Categories = await _mediator.Send(new GetArticleCategoriesQuery(), ct);
            return View(cmd);
        }
        TempData["Success"] = "دسته به‌روزرسانی شد";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("delete/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var r = await _mediator.Send(new DeleteArticleCategoryCommand(id), ct);
        TempData[r.Succeeded ? "Success" : "Error"] = r.Succeeded ? "دسته حذف شد" : r.Error;
        return RedirectToAction(nameof(Index));
    }
}