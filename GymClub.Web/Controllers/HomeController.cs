using GymClub.Application.Contact.Commands.SubmitContactMessage;
using GymClub.Application.Home.Queries.GetHomePageData;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GymClub.Web.Controllers;

public class HomeController : Controller
{
    private readonly IMediator _m;
    public HomeController(IMediator m) => _m = m;

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var data = await _m.Send(new GetHomePageDataQuery(), ct);
        return View(data);
    }

    [HttpGet("/about")]
    public IActionResult About() => View();

    [HttpGet("/contact")]
    public IActionResult Contact() => View(new SubmitContactMessageCommand("", "", null, "", ""));

    [HttpPost("/contact")]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("contact")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Contact(SubmitContactMessageCommand cmd, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(cmd);
        var r = await _m.Send(cmd, ct);
        if (!r.Succeeded) { ModelState.AddModelError("", r.Error!); return View(cmd); }
        TempData["ContactSuccess"] = "پیام شما با موفقیت ارسال شد. به‌زودی با شما تماس می‌گیریم.";
        return RedirectToAction(nameof(Contact));
    }

    // ═══════════ ۴۰۴ ═══════════
    [HttpGet("/not-found")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult NotFound(int code = 404)
    {
        ViewBag.Code = code == 0 ? 404 : code;

        // پیام مناسب بر اساس کد
        ViewBag.Title = code switch
        {
            401 => "دسترسی غیرمجاز",
            403 => "دسترسی ممنوع",
            404 => "صفحه یافت نشد",
            429 => "درخواست‌های بیش از حد",
            _ => "خطا"
        };
        ViewBag.Message = code switch
        {
            401 => "برای دسترسی به این بخش باید وارد شوید.",
            403 => "شما اجازه‌ی دسترسی به این بخش را ندارید.",
            404 => "صفحه‌ای که به دنبال آن بودید وجود ندارد یا حذف شده است.",
            429 => "تعداد درخواست‌های شما بیش از حد مجاز است. لطفاً کمی صبر کنید.",
            _ => "متأسفانه مشکلی پیش آمد."
        };

        Response.StatusCode = ViewBag.Code;
        return View();
    }

    // ═══════════ ۵۰۰ ═══════════
    [HttpGet("/error")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        var errorId = System.Diagnostics.Activity.Current?.Id
                   ?? HttpContext.TraceIdentifier;

        var exceptionFeature = HttpContext.Features
            .Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();

        if (exceptionFeature?.Error is not null)
        {
            var logger = HttpContext.RequestServices.GetRequiredService<ILogger<HomeController>>();
            logger.LogError(exceptionFeature.Error,
                "Unhandled exception at {Path} — ErrorId: {ErrorId}",
                exceptionFeature.Path, errorId);
        }

        ViewBag.ErrorId = errorId;
        return View();
    }
}