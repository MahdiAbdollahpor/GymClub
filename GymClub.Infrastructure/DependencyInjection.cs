using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Settings;
using GymClub.Domain.Entities;
using GymClub.Infrastructure.Persistence;
using GymClub.Infrastructure.Services;
using GymClub.Infrastructure.Services.FileStorage;
using GymClub.Infrastructure.Services.Sms;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GymClub.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
                sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddIdentity<User, IdentityRole<Guid>>(options =>
        {
            options.User.RequireUniqueEmail = false;
            options.Password.RequiredLength = 6;
            options.Password.RequireDigit = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.SignIn.RequireConfirmedPhoneNumber = false;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/auth/login";
            options.LogoutPath = "/auth/logout";
            options.AccessDeniedPath = "/auth/access-denied";
            options.ExpireTimeSpan = TimeSpan.FromDays(14);
            options.SlidingExpiration = true;
            options.Cookie.Name = "GymClub.Auth";
            options.Cookie.HttpOnly = true;
        });

        services.AddScoped<IDateTimeProvider, DateTimeProvider>();



        // داخل متد AddInfrastructure، قبل از return:

        services.Configure<OtpSettings>(configuration.GetSection("OtpSettings"));

        services.AddScoped<IOtpHasher, OtpHasher>();
        services.AddScoped<IOtpService, OtpService>();

        var smsProvider = configuration["SmsSettings:Provider"] ?? "Mock";
        if (smsProvider.Equals("Mock", StringComparison.OrdinalIgnoreCase))
            services.AddScoped<ISmsService, MockSmsService>();
        // در آینده: else if SmsIr/Kavenegar ...



        // ─── File Storage ───
        services.Configure<FileStorageOptions>(configuration.GetSection("FileStorage"));
        services.AddHttpClient<RemoteFileStorageService>();
        services.AddScoped<LocalFileStorageService>();

        services.AddScoped<IFileStorageService>(sp =>
        {
            var active = configuration["FileStorage:Active"] ?? "Local";
            return active.Equals("Remote", StringComparison.OrdinalIgnoreCase)
                ? sp.GetRequiredService<RemoteFileStorageService>()
                : sp.GetRequiredService<LocalFileStorageService>();
        });

        services.AddScoped<IImageStorageService, ImageStorageService>();
        services.AddScoped<IVideoStorageService, VideoStorageService>();

        return services;
    }
}