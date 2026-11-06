using GymClub.Application;
using GymClub.Application.Common.Interfaces;
using GymClub.Infrastructure;
using GymClub.Infrastructure.Persistence.Seed;
using GymClub.Web.Middleware;
using GymClub.Web.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Serilog;
using System.IO.Compression;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ═══════════════════════════════════════════════════════════════
// ۱. لاگ‌گیری (Serilog)
// ═══════════════════════════════════════════════════════════════
builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration));

// ═══════════════════════════════════════════════════════════════
// ۲. لایه‌ها (Clean Architecture)
// ═══════════════════════════════════════════════════════════════
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ═══════════════════════════════════════════════════════════════
// ۳. MVC + AntiForgery سراسری
// ═══════════════════════════════════════════════════════════════
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

// ═══════════════════════════════════════════════════════════════
// ۴. HttpContextAccessor + CurrentUserService
// ═══════════════════════════════════════════════════════════════
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// ═══════════════════════════════════════════════════════════════
// ۵. Authorization Policies
// ═══════════════════════════════════════════════════════════════
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

// ═══════════════════════════════════════════════════════════════
// ۶. Data Protection (کلیدهای پایدار)
// ═══════════════════════════════════════════════════════════════
var keysPath = Path.Combine(builder.Environment.ContentRootPath, "keys");
Directory.CreateDirectory(keysPath);

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90))
    .SetApplicationName("GymClub");

// ═══════════════════════════════════════════════════════════════
// ۷. SecurityStamp Validator (۳۰ دقیقه)
// ═══════════════════════════════════════════════════════════════
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.FromMinutes(30);
});

// ═══════════════════════════════════════════════════════════════
// ۸. Rate Limiting
// ═══════════════════════════════════════════════════════════════
builder.Services.AddRateLimiter(options =>
{
    // OTP — ۵ درخواست در ۱۰ دقیقه
    options.AddPolicy("otp", context =>
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(10),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        });
    });

    // لاگین — ۱۰ در ۵ دقیقه
    options.AddPolicy("login", context =>
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(5),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        });
    });

    // تماس — ۳ در ساعت
    options.AddPolicy("contact", context =>
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 3,
            Window = TimeSpan.FromHours(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        });
    });

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsync(
            "{\"success\":false,\"message\":\"تعداد درخواست‌ها بیش از حد مجاز. لطفاً بعداً تلاش کنید.\"}",
            cancellationToken);
    };
});

// ═══════════════════════════════════════════════════════════════
// ۹. Response Compression
// ═══════════════════════════════════════════════════════════════
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
    {
        "image/svg+xml", "application/json", "text/html"
    });
});

builder.Services.Configure<BrotliCompressionProviderOptions>(opts =>
{
    opts.Level = CompressionLevel.Fastest;
});
builder.Services.Configure<GzipCompressionProviderOptions>(opts =>
{
    opts.Level = CompressionLevel.Fastest;
});

// ═══════════════════════════════════════════════════════════════
// ۱۰. Build
// ═══════════════════════════════════════════════════════════════
var app = builder.Build();

// ═══════════════════════════════════════════════════════════════
// ۱۱. Seed داده‌ها
// ═══════════════════════════════════════════════════════════════
try
{
    await DatabaseSeeder.SeedAsync(app.Services);
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Program");
    logger.LogError(ex, "خطا در Seeder");
}

// ═══════════════════════════════════════════════════════════════
// ۱۲. Pipeline
// ═══════════════════════════════════════════════════════════════
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

// Status code pages (404 و ...)
app.UseStatusCodePagesWithReExecute("/Home/NotFound", "?code={0}");

// HTTPS + Compression + Security Headers
app.UseHttpsRedirection();
app.UseResponseCompression();
app.UseMiddleware<SecurityHeadersMiddleware>();

// Static files
app.UseStaticFiles();

// Routing
app.UseRouting();

// Rate Limiter (قبل از Auth یا بعد از؟ — بعد از Routing، قبل از Auth)
app.UseRateLimiter();

// Authentication + ActiveUser + Authorization
app.UseAuthentication();
app.UseMiddleware<ActiveUserMiddleware>();
app.UseAuthorization();

// ═══════════════════════════════════════════════════════════════
// ۱۳. Routes
// ═══════════════════════════════════════════════════════════════
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ═══════════════════════════════════════════════════════════════
// ۱۴. Run
// ═══════════════════════════════════════════════════════════════
try
{
    Log.Information("Starting GymClub Web...");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}