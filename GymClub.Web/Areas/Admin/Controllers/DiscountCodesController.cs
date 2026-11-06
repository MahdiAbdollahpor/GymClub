using GymClub.Application.Discounts.Commands.CreateDiscountCode;
using GymClub.Application.Discounts.Commands.DeleteDiscountCode;
using GymClub.Application.Discounts.Commands.ToggleActive;
using GymClub.Application.Discounts.Commands.UpdateDiscountCode;
using GymClub.Application.Discounts.Queries.GetAllDiscountCodes;
using GymClub.Application.Discounts.Queries.GetDiscountCodeForEdit;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymClub.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "ShopOnly")]   // ← دسترسی: مدیر فروشگاه
[Route("admin/discount-codes")]
public class DiscountCodesController : Controller
{
    private readonly IMediator _m;
    public DiscountCodesController(IMediator m) => _m = m;

    [HttpGet("")]
    public async Task<IActionResult> Index(string? search, bool? isActive, int page = 1, CancellationToken ct = default)
    {
        var list = await _m.Send(new GetAllDiscountCodesQuery(search, isActive, page), ct);
        ViewBag.Search = search;
        ViewBag.IsActive = isActive;
        return View(list);
    }

    [HttpGet("create")]
    public IActionResult Create()
    => View(new CreateDiscountCodeCommand("", 1, 0, null, null, null, null, null, true));
    

    [HttpPost("create")]
    public async Task<IActionResult> Create(CreateDiscountCodeCommand cmd, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(cmd);
        var r = await _m.Send(cmd, ct);
        if (!r.Succeeded) { ModelState.AddModelError("", r.Error!); return View(cmd); }
        TempData["Success"] = "کد تخفیف ایجاد شد";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("edit/{id:guid}")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var r = await _m.Send(new GetDiscountCodeForEditQuery(id), ct);
        if (!r.Succeeded) return NotFound();
        var d = r.Data!;
        return View(new UpdateDiscountCodeCommand(
            d.Id, d.Code, (int)d.Type, d.Value, d.MaxDiscountAmount, d.MinOrderAmount,
            d.MaxUsageCount, d.StartsAt, d.ExpiresAt, d.IsActive));
    }

    [HttpPost("edit/{id:guid}")]
    public async Task<IActionResult> Edit(Guid id, UpdateDiscountCodeCommand cmd, CancellationToken ct)
    {
        if (id != cmd.Id) return BadRequest();
        if (!ModelState.IsValid) return View(cmd);
        var r = await _m.Send(cmd, ct);
        if (!r.Succeeded) { ModelState.AddModelError("", r.Error!); return View(cmd); }
        TempData["Success"] = "کد تخفیف به‌روزرسانی شد";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("toggle/{id:guid}")]
    public async Task<IActionResult> Toggle(Guid id, CancellationToken ct)
    {
        var r = await _m.Send(new ToggleDiscountCodeActiveCommand(id), ct);
        TempData[r.Succeeded ? "Success" : "Error"] =
            r.Succeeded ? (r.Data ? "کد فعال شد" : "کد غیرفعال شد") : r.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("delete/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var r = await _m.Send(new DeleteDiscountCodeCommand(id), ct);
        TempData[r.Succeeded ? "Success" : "Error"] = r.Succeeded ? "کد تخفیف حذف شد" : r.Error;
        return RedirectToAction(nameof(Index));
    }
}