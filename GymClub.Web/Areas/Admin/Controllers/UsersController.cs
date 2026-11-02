using GymClub.Application.Users.Commands.ChangeUserRole;
using GymClub.Application.Users.Commands.CreateUserByAdmin;
using GymClub.Application.Users.Commands.ToggleUserActive;
using GymClub.Application.Users.Queries.GetAllUsers;
using GymClub.Application.Users.Queries.GetUserDetails;
using GymClub.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymClub.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "SuperAdminOnly")]
[Route("admin/users")]
public class UsersController : Controller
{
    private readonly IMediator _m;
    public UsersController(IMediator m) => _m = m;

    [HttpGet("")]
    public async Task<IActionResult> Index(string? search, int page = 1, CancellationToken ct = default)
    {
        var list = await _m.Send(new GetAllUsersQuery(search, page), ct);
        ViewBag.Search = search;
        return View(list);
    }

    [HttpGet("details/{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var r = await _m.Send(new GetUserDetailsQuery(id), ct);
        if (!r.Succeeded) return NotFound();
        return View(r.Data);
    }

    [HttpPost("toggle-active/{id:guid}")]
    public async Task<IActionResult> ToggleActive(Guid id, CancellationToken ct)
    {
        var r = await _m.Send(new ToggleUserActiveCommand(id), ct);
        TempData[r.Succeeded ? "Success" : "Error"] =
            r.Succeeded ? (r.Data ? "کاربر فعال شد" : "کاربر غیرفعال شد") : r.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("create")]
    [Authorize(Policy = "SuperAdminOnly")]
    public IActionResult Create() => View(new CreateUserByAdminCommand("", "", "", "", UserRole.User));

    [HttpPost("create")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> Create(CreateUserByAdminCommand cmd, CancellationToken ct)
    {

        Console.WriteLine($"[CreateUser] Role received: {cmd.Role} ({(int)cmd.Role})");
        if (!ModelState.IsValid) return View(cmd);
        var r = await _m.Send(cmd, ct);
        if (!r.Succeeded)
        {
            ModelState.AddModelError("", r.Error!);
            return View(cmd);
        }
        TempData["Success"] = "کاربر ایجاد شد";
        return RedirectToAction(nameof(Index));
    }


    [HttpPost("change-role/{id:guid}")]
    public async Task<IActionResult> ChangeRole(Guid id, UserRole newRole, CancellationToken ct)
    {
        Console.WriteLine($"[ChangeRole] id={id}, newRole={newRole} ({(int)newRole})");
  
    
    var r = await _m.Send(new ChangeUserRoleCommand(id, newRole), ct);
        TempData[r.Succeeded ? "Success" : "Error"] = r.Succeeded ? "نقش تغییر کرد" : r.Error;
        return RedirectToAction(nameof(Details), new { id });
    }
}