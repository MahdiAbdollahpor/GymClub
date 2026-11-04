using GymClub.Application.Contact.Commands.DeleteContactMessage;
using GymClub.Application.Contact.Commands.MarkContactMessageRead;
using GymClub.Application.Contact.Queries.GetContactMessages;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymClub.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
[Route("admin/contact-messages")]
public class ContactMessagesController : Controller
{
    private readonly IMediator _m;
    public ContactMessagesController(IMediator m) => _m = m;

    [HttpGet("")]
    public async Task<IActionResult> Index(bool? isRead, string? search, int page = 1, CancellationToken ct = default)
    {
        var list = await _m.Send(new GetContactMessagesQuery(isRead, search, page), ct);
        ViewBag.IsRead = isRead;
        ViewBag.Search = search;
        return View(list);
    }

    [HttpPost("mark-read/{id:guid}")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
    {
        var r = await _m.Send(new MarkContactMessageReadCommand(id), ct);
        TempData[r.Succeeded ? "Success" : "Error"] = r.Succeeded ? "خوانده‌شده علامت خورد" : r.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("delete/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var r = await _m.Send(new DeleteContactMessageCommand(id), ct);
        TempData[r.Succeeded ? "Success" : "Error"] = r.Succeeded ? "پیام حذف شد" : r.Error;
        return RedirectToAction(nameof(Index));
    }
}