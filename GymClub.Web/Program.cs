using GymClub.Application;
using GymClub.Application.Common.Interfaces;
using GymClub.Domain.Entities;
using GymClub.Infrastructure;
using GymClub.Infrastructure.Persistence.Seed;
using GymClub.Web.Middleware;
using GymClub.Web.Services;
using Microsoft.AspNetCore.Identity;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin", "SuperAdmin"));

    options.AddPolicy("SuperAdminOnly", policy =>
        policy.RequireRole("SuperAdmin"));

    options.AddPolicy("ContentOnly", policy =>
        policy.RequireRole("ContentWriter", "Admin", "SuperAdmin"));

    options.AddPolicy("ShopOnly", policy =>
        policy.RequireRole("ShopManager", "Admin", "SuperAdmin"));

    options.AddPolicy("TrainerOnly", policy =>
        policy.RequireRole("Trainer", "Admin", "SuperAdmin"));
});

builder.Services.AddControllersWithViews();

builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.FromSeconds(30);
});


builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();



var app = builder.Build();

// Middleware
app.Use(async (ctx, next) =>
{
    if (ctx.User.Identity?.IsAuthenticated == true)
    {
        var userManager = ctx.RequestServices.GetRequiredService<UserManager<User>>();
        var user = await userManager.GetUserAsync(ctx.User);
        if (user is not null && !user.IsActive)
        {
            var signInManager = ctx.RequestServices.GetRequiredService<SignInManager<User>>();
            await signInManager.SignOutAsync();
            ctx.Response.Redirect("/auth/login?disabled=true");
            return;
        }
    }
    await next();
});
// Seed دیتابیس
await DatabaseSeeder.SeedAsync(app.Services);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseMiddleware<RoleSyncMiddleware>();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();