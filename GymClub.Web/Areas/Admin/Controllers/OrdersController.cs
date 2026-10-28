using GymClub.Application.Orders.Commands.AdminOrderActions;
using GymClub.Application.Orders.Queries.GetAdminOrderDetails;
using GymClub.Application.Orders.Queries.GetAdminOrders;
using GymClub.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymClub.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
[Route("admin/orders")]
public class OrdersController : Controller
{
    private readonly IMediator _m;
    public OrdersController(IMediator m) => _m = m;

    [HttpGet("")]
    public async Task<IActionResult> Index(string? search, OrderStatus? status, int page = 1, CancellationToken ct = default)
    {
        var list = await _m.Send(new GetAdminOrdersQuery(search, status, page), ct);
        ViewBag.Search = search;
        ViewBag.Status = status;
        return View(list);
    }

    [HttpGet("details/{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var r = await _m.Send(new GetAdminOrderDetailsQuery(id), ct);
        if (!r.Succeeded) return NotFound();
        return View(r.Data);
    }

    [HttpPost("confirm/{id:guid}")]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken ct)
    {
        var r = await _m.Send(new ConfirmOrderCommand(id), ct);
        TempData[r.Succeeded ? "Success" : "Error"] = r.Succeeded ? "سفارش تأیید شد" : r.Error;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("ship/{id:guid}")]
    public async Task<IActionResult> Ship(Guid id, string trackingCode, CancellationToken ct)
    {
        var r = await _m.Send(new ShipOrderCommand(id, trackingCode), ct);
        TempData[r.Succeeded ? "Success" : "Error"] = r.Succeeded ? "سفارش ارسال شد" : r.Error;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("cancel/{id:guid}")]
    public async Task<IActionResult> Cancel(Guid id, string reason, CancellationToken ct)
    {
        var r = await _m.Send(new CancelOrderCommand(id, reason), ct);
        TempData[r.Succeeded ? "Success" : "Error"] = r.Succeeded ? "سفارش لغو شد" : r.Error;
        return RedirectToAction(nameof(Details), new { id });
    }
}