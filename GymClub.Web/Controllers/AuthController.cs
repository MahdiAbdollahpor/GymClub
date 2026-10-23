using GymClub.Application.Auth.Commands.ForgotPassword;
using GymClub.Application.Auth.Commands.Login;
using GymClub.Application.Auth.Commands.Register;
using GymClub.Application.Auth.Commands.VerifyOtp;
using GymClub.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GymClub.Web.Controllers;

public class AuthController : Controller
{
    private readonly IMediator _mediator;
    private readonly SignInManager<User> _signInManager;
    private readonly UserManager<User> _userManager;

    public AuthController(IMediator mediator, SignInManager<User> signInManager, UserManager<User> userManager)
    {
        _mediator = mediator; _signInManager = signInManager; _userManager = userManager;
    }

    // ---------- ثبت‌نام ----------
    [HttpGet("/auth/register")]
    public IActionResult Register() => View();

    [HttpPost("/auth/register")]
    public async Task<IActionResult> Register(RegisterCommand cmd, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(cmd);
        var result = await _mediator.Send(cmd, ct);
        if (!result.Succeeded)
        {
            ModelState.AddModelError("", result.Error!);
            return View(cmd);
        }
        TempData["Phone"] = cmd.PhoneNumber;
        return RedirectToAction(nameof(VerifyOtp));
    }

    // ---------- تأیید OTP ----------
    [HttpGet("/auth/verify-otp")]
    public IActionResult VerifyOtp()
    {
        if (TempData["Phone"] is null) return RedirectToAction(nameof(Register));
        ViewBag.Phone = TempData["Phone"];
        TempData.Keep("Phone");
        return View();
    }

    [HttpPost("/auth/verify-otp")]
    public async Task<IActionResult> VerifyOtp(string phoneNumber, string code, CancellationToken ct)
    {
        var result = await _mediator.Send(new VerifyOtpCommand(phoneNumber, code), ct);
        if (!result.Succeeded)
        {
            ModelState.AddModelError("", result.Error!);
            ViewBag.Phone = phoneNumber;
            TempData["Phone"] = phoneNumber;
            return View();
        }

        var user = await _userManager.FindByIdAsync(result.Data.ToString());
        if (user is not null)
            await SignInUserAsync(user, false);

        return RedirectToAction("Index", "Home");
    }

    // ---------- ورود ----------
    [HttpGet("/auth/login")]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost("/auth/login")]
    public async Task<IActionResult> Login(LoginCommand cmd, string? returnUrl, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(cmd);

        var result = await _mediator.Send(cmd, ct);
        if (!result.Succeeded)
        {
            ModelState.AddModelError("", result.Error!);
            return View(cmd);
        }

        var user = await _userManager.FindByIdAsync(result.Data.ToString());
        if (user is null) return RedirectToAction(nameof(Login));

        await SignInUserAsync(user, cmd.RememberMe);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return RedirectToAction("Index", "Home");
    }

    // ---------- خروج ----------
    [HttpPost("/auth/logout")]
    public async Task<IActionResult> Logout()
    {
        // ✅ از طرح Identity استفاده کنید، نه CookieAuthenticationDefaults
        await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
        return RedirectToAction("Index", "Home");
    }

    // ---------- فراموشی رمز ----------
    [HttpGet("/auth/forgot-password")]
    public IActionResult ForgotPassword() => View();

    [HttpPost("/auth/forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordCommand cmd, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(cmd);
        await _mediator.Send(cmd, ct);
        TempData["Phone"] = cmd.PhoneNumber;
        TempData["ResetSent"] = true;
        return RedirectToAction(nameof(ResetPassword));
    }

    [HttpGet("/auth/reset-password")]
    public IActionResult ResetPassword()
    {
        ViewBag.Phone = TempData["Phone"];
        TempData.Keep("Phone");
        return View();
    }

    [HttpPost("/auth/reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordCommand cmd, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(cmd);
        var result = await _mediator.Send(cmd, ct);
        if (!result.Succeeded)
        {
            ModelState.AddModelError("", result.Error!);
            return View(cmd);
        }
        TempData["ResetDone"] = true;
        return RedirectToAction(nameof(Login));
    }

    [HttpGet("/auth/access-denied")]
    public IActionResult AccessDenied() => View();

    // ---------- Helper ----------
    private async Task SignInUserAsync(User user, bool rememberMe)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new(ClaimTypes.Name, user.PhoneNumber ?? ""),
    };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        // ✅ استفاده از طرح Identity.Application
        var identity = new ClaimsIdentity(claims, IdentityConstants.ApplicationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            IdentityConstants.ApplicationScheme,  // ✅ کلید تغییر اینجاست
            principal,
            new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(14) : null
            });
    }
}