using GymClub.Application.Seo.Queries.GetSitemapUrls;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace GymClub.Web.Controllers;

public class SeoController : Controller
{
    private readonly IMediator _m;
    public SeoController(IMediator m) => _m = m;

    [HttpGet("/sitemap.xml")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Sitemap(CancellationToken ct)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var urls = await _m.Send(new GetSitemapUrlsQuery(baseUrl), ct);

        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

        foreach (var u in urls)
        {
            sb.AppendLine("  <url>");
            sb.AppendLine($"    <loc>{System.Security.SecurityElement.Escape(u.Location)}</loc>");
            if (u.LastModified.HasValue)
                sb.AppendLine($"    <lastmod>{u.LastModified.Value:yyyy-MM-dd}</lastmod>");
            if (!string.IsNullOrEmpty(u.ChangeFreq))
                sb.AppendLine($"    <changefreq>{u.ChangeFreq}</changefreq>");
            if (u.Priority.HasValue)
                sb.AppendLine($"    <priority>{u.Priority.Value.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)}</priority>");
            sb.AppendLine("  </url>");
        }

        sb.AppendLine("</urlset>");

        return Content(sb.ToString(), "application/xml", Encoding.UTF8);
    }

    [HttpGet("/robots.txt")]
    public IActionResult Robots()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var content = $@"User-agent: *
Allow: /
Disallow: /admin/
Disallow: /cart
Disallow: /checkout
Disallow: /payment/
Disallow: /account/
Disallow: /workout/answers

Sitemap: {baseUrl}/sitemap.xml
";
        return Content(content, "text/plain", Encoding.UTF8);
    }
}