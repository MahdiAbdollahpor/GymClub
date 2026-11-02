using GymClub.Application.Dashboard.Queries.GetAdminDashboardStats;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymClub.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]   // ← Admin و SuperAdmin
[Route("admin")]
public class DashboardController : Controller
{
    private readonly IMediator _m;
    public DashboardController(IMediator m) => _m = m;

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        // اگر Admin یا SuperAdmin است، داشبورد کامل
        if (User.IsInRole("Admin") || User.IsInRole("SuperAdmin"))
        {
            var stats = await _m.Send(new GetAdminDashboardStatsQuery(), ct);
            return View(stats);
        }

        // بقیه نقش‌ها → داشبورد اختصاصی خودشان
        return RedirectToDashboardByRole();
    }

    // ─── داشبورد نویسنده محتوا ───
    [HttpGet("content")]
    [Authorize(Policy = "ContentOnly")]
    public IActionResult Content() => View();

    // ─── داشبورد مدیر فروشگاه ───
    [HttpGet("shop")]
    [Authorize(Policy = "ShopOnly")]
    public IActionResult Shop() => View();

    // ─── داشبورد مربی ───
    [HttpGet("trainer")]
    [Authorize(Policy = "TrainerOnly")]
    public IActionResult Trainer() => View();

    private IActionResult RedirectToDashboardByRole()
    {
        if (User.IsInRole("ContentWriter")) return RedirectToAction(nameof(Content));
        if (User.IsInRole("ShopManager")) return RedirectToAction(nameof(Shop));
        if (User.IsInRole("Trainer")) return RedirectToAction(nameof(Trainer));
        return RedirectToAction("AccessDenied", "Auth", new { area = "" });
    }
}