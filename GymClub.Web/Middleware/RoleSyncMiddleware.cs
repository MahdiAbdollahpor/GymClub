using System.Security.Claims;
using GymClub.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace GymClub.Web.Middleware;

public class RoleSyncMiddleware
{
    private readonly RequestDelegate _next;

    public RoleSyncMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(
        HttpContext context,
        UserManager<User> userManager,
        SignInManager<User> signInManager)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var user = await userManager.GetUserAsync(context.User);

            if (user is null || !user.IsActive)
            {
                await signInManager.SignOutAsync();
                context.Response.Redirect("/auth/login");
                return;
            }

            // نقش‌های فعلی از دیتابیس
            var dbRoles = (await userManager.GetRolesAsync(user))
                          .OrderBy(x => x).ToList();

            // نقش‌های کوکی
            var cookieRoles = context.User.FindAll(ClaimTypes.Role)
                              .Select(c => c.Value)
                              .OrderBy(x => x).ToList();

            if (!dbRoles.SequenceEqual(cookieRoles))
            {
                // نقش‌ها تغییر کرده — خروج و ورود مجدد
                await signInManager.SignOutAsync();
                context.Response.Redirect("/auth/login?roleChanged=true");
                return;
            }
        }

        await _next(context);
    }
}