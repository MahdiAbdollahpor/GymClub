using GymClub.Application.Articles.Commands.CreateArticle;
using GymClub.Application.Articles.Commands.DeleteArticle;
using GymClub.Application.Articles.Commands.UpdateArticle;
using GymClub.Application.Articles.Dtos;
using GymClub.Application.Articles.Queries.GetAllArticlesForAdmin;
using GymClub.Application.Articles.Queries.GetArticleCategories;
using GymClub.Application.Articles.Queries.GetArticleForEdit;
using GymClub.Domain.Enums;
using GymClub.Web.Areas.Admin.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

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
        await FillCategoriesAsync(ct);
        return View(new ArticleFormViewModel());
    }

    [HttpPost("create")]
    [RequestSizeLimit(600 * 1024 * 1024)] // 600MB برای ویدیو
    public async Task<IActionResult> Create(ArticleFormViewModel vm, CancellationToken ct)
    {
        ValidateFiles(vm);

        if (!ModelState.IsValid)
        {
            await FillCategoriesAsync(ct);
            return View(vm);
        }

        using var coverStream = vm.CoverImageFile?.OpenReadStream();
        using var videoStream = vm.VideoFile?.OpenReadStream();

        var cmd = new CreateArticleCommand(
            vm.Title, vm.Summary, vm.ContentHtml, vm.Type, vm.CategoryId, vm.Publish,
            coverStream, vm.CoverImageFile?.FileName, vm.CoverImageFile?.ContentType,
            videoStream, vm.VideoFile?.FileName, vm.VideoFile?.ContentType);

        var r = await _mediator.Send(cmd, ct);
        if (!r.Succeeded)
        {
            ModelState.AddModelError("", r.Error!);
            await FillCategoriesAsync(ct);
            return View(vm);
        }

        TempData["Success"] = "مقاله ایجاد شد";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("edit/{id:guid}")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var r = await _mediator.Send(new GetArticleForEditQuery(id), ct);
        if (!r.Succeeded) return NotFound();

        await FillCategoriesAsync(ct);
        var d = r.Data!;
        return View(new ArticleFormViewModel
        {
            Id = d.Id,
            Title = d.Title,
            Summary = d.Summary,
            ContentHtml = d.ContentHtml,
            Type = d.Type,
            CategoryId = d.CategoryId,
            Publish = d.PublishedAt.HasValue,
            CurrentCoverImageUrl = d.CoverImageUrl,
            CurrentVideoUrl = d.VideoUrl
        });
    }

    [HttpPost("edit/{id:guid}")]
    [RequestSizeLimit(600 * 1024 * 1024)]
    public async Task<IActionResult> Edit(Guid id, ArticleFormViewModel vm, CancellationToken ct)
    {
        if (vm.Id != id) return BadRequest();

        ValidateFiles(vm);

        if (!ModelState.IsValid)
        {
            await FillCategoriesAsync(ct);
            return View(vm);
        }

        using var coverStream = vm.CoverImageFile?.OpenReadStream();
        using var videoStream = vm.VideoFile?.OpenReadStream();

        var cmd = new UpdateArticleCommand(
            id, vm.Title, vm.Summary, vm.ContentHtml, vm.Type, vm.CategoryId, vm.Publish,
            coverStream, vm.CoverImageFile?.FileName, vm.CoverImageFile?.ContentType,
            videoStream, vm.VideoFile?.FileName, vm.VideoFile?.ContentType,
            vm.RemoveCoverImage, vm.RemoveVideo);

        var r = await _mediator.Send(cmd, ct);
        if (!r.Succeeded)
        {
            ModelState.AddModelError("", r.Error!);
            await FillCategoriesAsync(ct);
            return View(vm);
        }

        TempData["Success"] = "مقاله به‌روزرسانی شد";
        return RedirectToAction(nameof(Index));
    }


    [HttpGet("details/{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var r = await _mediator.Send(new GetArticleForEditQuery(id), ct);
        if (!r.Succeeded) return NotFound();
        return View(r.Data);
    }

    [HttpPost("delete/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var r = await _mediator.Send(new DeleteArticleCommand(id), ct);
        TempData[r.Succeeded ? "Success" : "Error"] = r.Succeeded ? "مقاله حذف شد" : r.Error;
        return RedirectToAction(nameof(Index));
    }

    // ─── Helpers ───
    private void ValidateFiles(ArticleFormViewModel vm)
    {
        if (vm.CoverImageFile is not null)
        {
            var allowed = new[] { "image/jpeg", "image/png", "image/webp", "image/gif" };
            if (!allowed.Contains(vm.CoverImageFile.ContentType))
                ModelState.AddModelError(nameof(vm.CoverImageFile), "فرمت عکس مجاز نیست (jpg/png/webp/gif)");
            if (vm.CoverImageFile.Length > 5 * 1024 * 1024)
                ModelState.AddModelError(nameof(vm.CoverImageFile), "حجم عکس حداکثر ۵ مگابایت");
        }

        if (vm.VideoFile is not null)
        {
            var allowed = new[] { "video/mp4", "video/webm", "video/quicktime" };
            if (!allowed.Contains(vm.VideoFile.ContentType))
                ModelState.AddModelError(nameof(vm.VideoFile), "فرمت ویدیو مجاز نیست (mp4/webm/mov)");
            if (vm.VideoFile.Length > 500L * 1024 * 1024)
                ModelState.AddModelError(nameof(vm.VideoFile), "حجم ویدیو حداکثر ۵۰۰ مگابایت");
        }
    }

    private async Task FillCategoriesAsync(CancellationToken ct)
    {
        var cats = await _mediator.Send(new GetArticleCategoriesQuery(), ct);
        ViewBag.Categories = cats;
        ViewBag.CategoryOptions = cats
            .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Title })
            .ToList();
    }
}