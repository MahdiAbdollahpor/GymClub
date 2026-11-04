using GymClub.Application.Contact.Commands.SubmitContactMessage;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GymClub.Web.Controllers;

public class HomeController : Controller
{
    private readonly IMediator _m;
    public HomeController(IMediator m) => _m = m;

    public IActionResult Index() => View();

    [HttpGet("/about")]
    public IActionResult About() => View();

    [HttpGet("/contact")]
    public IActionResult Contact() => View(new SubmitContactMessageCommand("", "", null, "", ""));

    [HttpPost("/contact")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Contact(SubmitContactMessageCommand cmd, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(cmd);

        var r = await _m.Send(cmd, ct);
        if (!r.Succeeded)
        {
            ModelState.AddModelError("", r.Error!);
            return View(cmd);
        }

        TempData["ContactSuccess"] = "پیام شما با موفقیت ارسال شد. به‌زودی با شما تماس می‌گیریم.";
        return RedirectToAction(nameof(Contact));
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View();
}