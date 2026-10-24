using GymClub.Application.Products.Queries.GetActiveProducts;
using GymClub.Application.Products.Queries.GetProductBySlug;
using GymClub.Application.Products.Queries.GetProductCategories;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GymClub.Web.Controllers;

public class ProductsController : Controller
{
    private readonly IMediator _mediator;
    public ProductsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("/products")]
    public async Task<IActionResult> Index(
        Guid? categoryId, string? search, decimal? minPrice, decimal? maxPrice,
        string? sort, int page = 1, CancellationToken ct = default)
    {
        var list = await _mediator.Send(new GetActiveProductsQuery(
            categoryId, search, minPrice, maxPrice, sort, page), ct);
        var cats = await _mediator.Send(new GetProductCategoriesQuery(), ct);

        ViewBag.Categories = cats;
        ViewBag.CategoryId = categoryId;
        ViewBag.Search = search;
        ViewBag.MinPrice = minPrice;
        ViewBag.MaxPrice = maxPrice;
        ViewBag.Sort = sort;
        return View(list);
    }

    [HttpGet("/products/{slug}")]
    public async Task<IActionResult> Details(string slug, CancellationToken ct)
    {
        var r = await _mediator.Send(new GetProductBySlugQuery(slug), ct);
        if (!r.Succeeded) return NotFound();
        return View(r.Data);
    }
}