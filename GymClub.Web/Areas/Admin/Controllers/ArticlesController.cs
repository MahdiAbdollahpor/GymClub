using GymClub.Application.Articles.Commands.CreateArticle;
using GymClub.Application.Articles.Commands.DeleteArticle;
using GymClub.Application.Articles.Commands.UpdateArticle;
using GymClub.Application.Articles.Queries.GetAllArticlesForAdmin;
using GymClub.Application.Articles.Queries.GetArticleCategories;
using GymClub.Application.Articles.Queries.GetArticleForEdit;
using GymClub.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymClub.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
[Route("admin/articles")]
public class ArticlesController : Controller
{
    private readonly IMediator _mediator;
    public ArticlesController(IMediator mediator) => _mediator = mediator;

    [HttpGet("")]
    public async Task<IActionResult> Index(string? search, ArticleStatus? status, int page = 1, CancellationToken ct = default)
    {
        var list = await _mediator.Send(new GetAllArticlesForAdminQuery(search, status, page), ct);
        ViewBag.Search = search;
        ViewBag.Status = status;
        return View(list);
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        ViewBag.Categories = await _mediator.Send(new GetArticleCategoriesQuery(), ct);
        return View(new CreateArticleCommand("", "", "", null, null, ArticleType.Training, null, false));
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create(CreateArticleCommand cmd, CancellationToken ct)
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
        TempData["Success"] = "مقاله ایجاد شد";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("edit/{id:guid}")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var r = await _mediator.Send(new GetArticleForEditQuery(id), ct);
        if (!r.Succeeded) return NotFound();

        ViewBag.Categories = await _mediator.Send(new GetArticleCategoriesQuery(), ct);
        var d = r.Data!;
        return View(new UpdateArticleCommand(
            d.Id, d.Title, d.Summary, d.ContentHtml, d.CoverImageUrl, d.VideoUrl,
            d.Type, d.CategoryId, d.PublishedAt.HasValue));
    }

    [HttpPost("edit/{id:guid}")]
    public async Task<IActionResult> Edit(Guid id, UpdateArticleCommand cmd, CancellationToken ct)
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
        TempData["Success"] = "مقاله به‌روزرسانی شد";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("delete/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var r = await _mediator.Send(new DeleteArticleCommand(id), ct);
        TempData[r.Succeeded ? "Success" : "Error"] = r.Succeeded ? "مقاله حذف شد" : r.Error;
        return RedirectToAction(nameof(Index));
    }
}