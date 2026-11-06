using GymClub.Application.Search.Queries.GlobalSearch;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GymClub.Web.Controllers;

public class SearchController : Controller
{
    private readonly IMediator _m;
    public SearchController(IMediator m) => _m = m;

    [HttpGet("/search")]
    public async Task<IActionResult> Index(string? q, CancellationToken ct)
    {
        var result = await _m.Send(new GlobalSearchQuery(q ?? ""), ct);
        return View(result);
    }

    // برای Autocomplete (اختیاری)
    [HttpGet("/search/suggest")]
    public async Task<IActionResult> Suggest(string? q, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
            return Json(new { articles = Array.Empty<object>(), products = Array.Empty<object>() });

        var r = await _m.Send(new GlobalSearchQuery(q, PerSectionLimit: 5), ct);
        return Json(new
        {
            query = q,
            articles = r.Articles.Select(a => new { a.Title, a.Slug, a.CategoryTitle, a.CoverImageUrl }),
            products = r.Products.Select(p => new { p.Name, p.Slug, p.CategoryTitle, p.MainImageUrl, price = p.FinalPrice })
        });
    }
}