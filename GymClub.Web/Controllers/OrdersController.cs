using GymClub.Application.Orders.Queries.GetOrderDetails;
using GymClub.Application.Orders.Queries.GetUserOrders;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymClub.Web.Controllers;

[Authorize]
public class OrdersController : Controller
{
    private readonly IMediator _m;
    public OrdersController(IMediator m) => _m = m;

    [HttpGet("/orders")]
    public async Task<IActionResult> Index(CancellationToken ct)
        => View(await _m.Send(new GetUserOrdersQuery(), ct));

    [HttpGet("/orders/{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var r = await _m.Send(new GetOrderDetailsQuery(id), ct);
        if (!r.Succeeded) return NotFound();
        return View(r.Data);
    }
}