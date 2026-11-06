using GymClub.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace GymClub.Web.Middleware;

public class ActiveUserMiddleware
{
    private readonly RequestDelegate _next;

    public ActiveUserMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, UserManager<User> userManager, SignInManager<User> signInManager)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var user = await userManager.GetUserAsync(context.User);

            if (user is null || !user.IsActive)
            {
                await signInManager.SignOutAsync();

                // اگر API است، JSON برگردان
                if (context.Request.Path.StartsWithSegments("/api") ||
                    context.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    context.Response.StatusCode = 401;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(
                        "{\"success\":false,\"message\":\"حساب شما غیرفعال شده است.\"}");
                    return;
                }

                context.Response.Redirect("/auth/login?disabled=true");
                return;
            }
        }

        await _next(context);
    }
}