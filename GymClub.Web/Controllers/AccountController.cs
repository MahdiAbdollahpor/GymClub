using GymClub.Application.Users.Commands.AddAddress;
using GymClub.Application.Users.Commands.ChangePassword;
using GymClub.Application.Users.Commands.DeleteAddress;
using GymClub.Application.Users.Commands.SetDefaultAddress;
using GymClub.Application.Users.Commands.UpdateAddress;
using GymClub.Application.Users.Commands.UpdateProfile;
using GymClub.Application.Users.Queries.GetAddressById;
using GymClub.Application.Users.Queries.GetAddresses;
using GymClub.Application.Users.Queries.GetProfile;
using GymClub.Application.Users.Queries.GetUserDashboard;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymClub.Web.Controllers;

[Authorize]
[Route("account")]
public class AccountController : Controller
{
    private readonly IMediator _m;
    public AccountController(IMediator m) => _m = m;

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var dash = await _m.Send(new GetUserDashboardQuery(), ct);
        var profile = await _m.Send(new GetProfileQuery(), ct);
        ViewBag.Profile = profile.Data;
        return View(dash);
    }

    // ─── پروفایل ───
    [HttpGet("profile")]
    public async Task<IActionResult> Profile(CancellationToken ct)
    {
        var r = await _m.Send(new GetProfileQuery(), ct);
        if (!r.Succeeded) return RedirectToAction(nameof(Index));
        return View(new UpdateProfileCommand(r.Data!.DisplayUserName, r.Data.FullName));
    }

    [HttpPost("profile")]
    public async Task<IActionResult> Profile(UpdateProfileCommand cmd, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(cmd);
        var r = await _m.Send(cmd, ct);
        if (!r.Succeeded)
        {
            ModelState.AddModelError("", r.Error!);
            return View(cmd);
        }
        TempData["Success"] = "پروفایل به‌روزرسانی شد";
        return RedirectToAction(nameof(Index));
    }

    // ─── تغییر رمز ───
    [HttpGet("change-password")]
    public IActionResult ChangePassword() => View();

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordCommand cmd, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(cmd);
        var r = await _m.Send(cmd, ct);
        if (!r.Succeeded)
        {
            ModelState.AddModelError("", r.Error!);
            return View(cmd);
        }
        TempData["Success"] = "رمز عبور تغییر کرد";
        return RedirectToAction(nameof(Index));
    }

    // ─── آدرس‌ها ───
    [HttpGet("addresses")]
    public async Task<IActionResult> Addresses(CancellationToken ct)
    {
        var list = await _m.Send(new GetAddressesQuery(), ct);
        return View(list);
    }

    [HttpGet("addresses/create")]
    public IActionResult CreateAddress() => View(new AddAddressCommand("", "", "", "", "", "", "", false));

    [HttpPost("addresses/create")]
    public async Task<IActionResult> CreateAddress(AddAddressCommand cmd, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(cmd);
        var r = await _m.Send(cmd, ct);
        if (!r.Succeeded)
        {
            ModelState.AddModelError("", r.Error!);
            return View(cmd);
        }
        TempData["Success"] = "آدرس اضافه شد";
        return RedirectToAction(nameof(Addresses));
    }

    [HttpGet("addresses/edit/{id:guid}")]
    public async Task<IActionResult> EditAddress(Guid id, CancellationToken ct)
    {
        var r = await _m.Send(new GetAddressByIdQuery(id), ct);
        if (!r.Succeeded) return NotFound();
        var a = r.Data!;
        return View(new UpdateAddressCommand(
            a.Id, a.Title, a.Province, a.City, a.PostalCode,
            a.FullAddress, a.ReceiverName, a.ReceiverPhone, a.IsDefault));
    }

    [HttpPost("addresses/edit/{id:guid}")]
    public async Task<IActionResult> EditAddress(Guid id, UpdateAddressCommand cmd, CancellationToken ct)
    {
        if (id != cmd.Id) return BadRequest();
        if (!ModelState.IsValid) return View(cmd);
        var r = await _m.Send(cmd, ct);
        if (!r.Succeeded)
        {
            ModelState.AddModelError("", r.Error!);
            return View(cmd);
        }
        TempData["Success"] = "آدرس به‌روزرسانی شد";
        return RedirectToAction(nameof(Addresses));
    }

    [HttpPost("addresses/delete/{id:guid}")]
    public async Task<IActionResult> DeleteAddress(Guid id, CancellationToken ct)
    {
        var r = await _m.Send(new DeleteAddressCommand(id), ct);
        TempData[r.Succeeded ? "Success" : "Error"] = r.Succeeded ? "آدرس حذف شد" : r.Error;
        return RedirectToAction(nameof(Addresses));
    }

    [HttpPost("addresses/set-default/{id:guid}")]
    public async Task<IActionResult> SetDefaultAddress(Guid id, CancellationToken ct)
    {
        var r = await _m.Send(new SetDefaultAddressCommand(id), ct);
        TempData[r.Succeeded ? "Success" : "Error"] = r.Succeeded ? "آدرس پیش‌فرض شد" : r.Error;
        return RedirectToAction(nameof(Addresses));
    }
}