using GymClub.Application.Products.Commands.CreateProduct;
using GymClub.Application.Products.Commands.DeleteProduct;
using GymClub.Application.Products.Commands.UpdateProduct;
using GymClub.Application.Products.Queries.GetAllProductsForAdmin;
using GymClub.Application.Products.Queries.GetProductCategories;
using GymClub.Application.Products.Queries.GetProductForEdit;
using GymClub.Web.Areas.Admin.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GymClub.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
[Route("admin/products")]
public class ProductsController : Controller
{
    private readonly IMediator _mediator;
    public ProductsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("")]
    public async Task<IActionResult> Index(string? search, Guid? categoryId, int page = 1, CancellationToken ct = default)
    {
        var list = await _mediator.Send(new GetAllProductsForAdminQuery(search, categoryId, page), ct);
        var cats = await _mediator.Send(new GetProductCategoriesQuery(OnlyActive: false), ct);
        ViewBag.Categories = cats;
        ViewBag.Search = search;
        ViewBag.CategoryId = categoryId;
        return View(list);
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        await FillCategories(ct);
        return View(new ProductFormViewModel());
    }

    [HttpPost("create")]
    [RequestSizeLimit(100 * 1024 * 1024)]
    public async Task<IActionResult> Create(ProductFormViewModel vm, CancellationToken ct)
    {
        Validate(vm);
        if (!ModelState.IsValid) { await FillCategories(ct); return View(vm); }

        var images = new List<NewProductImage>();
        foreach (var f in vm.Images ?? new())
        {
            images.Add(new NewProductImage
            {
                Stream = f.OpenReadStream(),
                FileName = f.FileName,
                ContentType = f.ContentType,
                IsMain = f == vm.Images!.First()
            });
        }

        var attrs = (vm.Attributes ?? new())
            .Where(a => !string.IsNullOrWhiteSpace(a.Title) && !string.IsNullOrWhiteSpace(a.Value))
            .Select(a => new NewProductAttribute { Title = a.Title, Value = a.Value })
            .ToList();

        var cmd = new CreateProductCommand(
            vm.Name, vm.Description, vm.CategoryId, vm.Price, vm.DiscountPrice,
            vm.Stock, vm.IsActive, images, attrs);

        var r = await _mediator.Send(cmd, ct);
        if (!r.Succeeded)
        {
            ModelState.AddModelError("", r.Error!);
            await FillCategories(ct);
            return View(vm);
        }
        TempData["Success"] = "محصول ایجاد شد";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("edit/{id:guid}")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var r = await _mediator.Send(new GetProductForEditQuery(id), ct);
        if (!r.Succeeded) return NotFound();

        await FillCategories(ct);
        var d = r.Data!;
        return View(new ProductFormViewModel
        {
            Id = d.Id,
            Name = d.Name,
            Description = d.Description,
            CategoryId = d.CategoryId,
            Price = d.Price,
            DiscountPrice = d.DiscountPrice,
            Stock = d.Stock,
            IsActive = true,
            ExistingImages = d.Images,
            Attributes = d.Attributes.Select(a => new ProductAttributeInput { Title = a.Title, Value = a.Value }).ToList()
        });
    }

    [HttpPost("edit/{id:guid}")]
    [RequestSizeLimit(100 * 1024 * 1024)]
    public async Task<IActionResult> Edit(Guid id, ProductFormViewModel vm, CancellationToken ct)
    {
        if (vm.Id != id) return BadRequest();
        Validate(vm);
        if (!ModelState.IsValid) { await FillCategories(ct); return View(vm); }

        var newImages = new List<NewProductImage>();
        foreach (var f in vm.NewImages ?? new())
        {
            newImages.Add(new NewProductImage
            {
                Stream = f.OpenReadStream(),
                FileName = f.FileName,
                ContentType = f.ContentType
            });
        }

        var attrs = (vm.Attributes ?? new())
            .Where(a => !string.IsNullOrWhiteSpace(a.Title) && !string.IsNullOrWhiteSpace(a.Value))
            .Select(a => new NewProductAttribute { Title = a.Title, Value = a.Value })
            .ToList();

        var cmd = new UpdateProductCommand(
            id, vm.Name, vm.Description, vm.CategoryId, vm.Price, vm.DiscountPrice,
            vm.Stock, vm.IsActive, newImages, attrs,
            vm.RemoveImageIds, vm.SetMainImageId);

        var r = await _mediator.Send(cmd, ct);
        if (!r.Succeeded)
        {
            ModelState.AddModelError("", r.Error!);
            await FillCategories(ct);
            return View(vm);
        }
        TempData["Success"] = "محصول به‌روزرسانی شد";
        return RedirectToAction(nameof(Index));
    }


    [HttpGet("details/{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var r = await _mediator.Send(new GetProductForEditQuery(id), ct);
        if (!r.Succeeded) return NotFound();
        return View(r.Data);
    }

    [HttpPost("delete/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var r = await _mediator.Send(new DeleteProductCommand(id), ct);
        TempData[r.Succeeded ? "Success" : "Error"] = r.Succeeded ? "محصول حذف شد" : r.Error;
        return RedirectToAction(nameof(Index));
    }

    private void Validate(ProductFormViewModel vm)
    {
        if (vm.Images != null)
            foreach (var f in vm.Images)
                if (f.Length > 5 * 1024 * 1024)
                    ModelState.AddModelError(nameof(vm.Images), "حجم هر عکس حداکثر ۵ مگابایت");
        if (vm.NewImages != null)
            foreach (var f in vm.NewImages)
                if (f.Length > 5 * 1024 * 1024)
                    ModelState.AddModelError(nameof(vm.NewImages), "حجم هر عکس حداکثر ۵ مگابایت");
    }

    private async Task FillCategories(CancellationToken ct)
    {
        var cats = await _mediator.Send(new GetProductCategoriesQuery(OnlyActive: false), ct);
        ViewBag.Categories = cats;
        ViewBag.CategoryOptions = cats
            .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Title })
            .ToList();
    }
}