using GymClub.Application.Articles.Queries.GetArticleBySlug;
using GymClub.Application.Articles.Queries.GetArticleCategories;
using GymClub.Application.Articles.Queries.GetPublishedArticles;
using GymClub.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GymClub.Web.Controllers;

public class ArticlesController : Controller
{
    private readonly IMediator _mediator;
    public ArticlesController(IMediator mediator) => _mediator = mediator;

    [HttpGet("/articles")]
    public async Task<IActionResult> Index(
        Guid? categoryId, ArticleType? type, string? search, int page = 1, CancellationToken ct = default)
    {
        var list = await _mediator.Send(new GetPublishedArticlesQuery(categoryId, type, search, page), ct);
        var cats = await _mediator.Send(new GetArticleCategoriesQuery(), ct);

        ViewBag.Categories = cats;
        ViewBag.CategoryId = categoryId;
        ViewBag.Type = type;
        ViewBag.Search = search;

        return View(list);
    }

    [HttpGet("/articles/{slug}")]
    public async Task<IActionResult> Details(string slug, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetArticleBySlugQuery(slug), ct);
        if (!result.Succeeded) return NotFound();
        return View(result.Data);
    }
}