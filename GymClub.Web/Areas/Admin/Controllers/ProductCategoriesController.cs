using GymClub.Application.ProductCategories.Commands.Create;
using GymClub.Application.ProductCategories.Commands.Delete;
using GymClub.Application.ProductCategories.Commands.Update;
using GymClub.Application.ProductCategories.Queries.GetForEdit;
using GymClub.Application.ProductCategories.Queries.GetProductCategoryDetails;
using GymClub.Application.Products.Queries.GetProductCategories;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace GymClub.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
[Route("admin/product-categories")]
public class ProductCategoriesController : Controller
{
    private readonly IMediator _m;
    public ProductCategoriesController(IMediator m) => _m = m;

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
        => View(await _m.Send(new GetProductCategoriesQuery(OnlyActive: false), ct));

    [HttpGet("create")]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        ViewBag.Categories = await _m.Send(new GetProductCategoriesQuery(false), ct);
        return View(new CreateProductCategoryCommand("", null, 0, true));
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create(CreateProductCategoryCommand cmd, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Categories = await _m.Send(new GetProductCategoriesQuery(false), ct);
            return View(cmd);
        }
        var r = await _m.Send(cmd, ct);
        if (!r.Succeeded)
        {
            ModelState.AddModelError("", r.Error!);
            ViewBag.Categories = await _m.Send(new GetProductCategoriesQuery(false), ct);
            return View(cmd);
        }
        TempData["Success"] = "دسته ایجاد شد";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("edit/{id:guid}")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var r = await _m.Send(new GetProductCategoryForEditQuery(id), ct);
        if (!r.Succeeded) return NotFound();
        ViewBag.Categories = await _m.Send(new GetProductCategoriesQuery(false), ct);
        var d = r.Data!;
        return View(new UpdateProductCategoryCommand(d.Id, d.Title, d.ParentId, d.SortOrder, d.IsActive));
    }

    [HttpPost("edit/{id:guid}")]
    public async Task<IActionResult> Edit(Guid id, UpdateProductCategoryCommand cmd, CancellationToken ct)
    {
        if (id != cmd.Id) return BadRequest();
        if (!ModelState.IsValid)
        {
            ViewBag.Categories = await _m.Send(new GetProductCategoriesQuery(false), ct);
            return View(cmd);
        }
        var r = await _m.Send(cmd, ct);
        if (!r.Succeeded)
        {
            ModelState.AddModelError("", r.Error!);
            ViewBag.Categories = await _m.Send(new GetProductCategoriesQuery(false), ct);
            return View(cmd);
        }
        TempData["Success"] = "دسته به‌روزرسانی شد";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("details/{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var r = await _m.Send(new GetProductCategoryDetailsQuery(id), ct);
        if (!r.Succeeded) return NotFound();
        return View(r.Data);
    }

    [HttpPost("delete/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var r = await _m.Send(new DeleteProductCategoryCommand(id), ct);
        TempData[r.Succeeded ? "Success" : "Error"] = r.Succeeded ? "دسته حذف شد" : r.Error;
        return RedirectToAction(nameof(Index));
    }
}