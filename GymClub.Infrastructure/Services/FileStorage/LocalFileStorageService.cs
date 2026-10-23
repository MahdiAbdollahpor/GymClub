using GymClub.Application.Common.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace GymClub.Infrastructure.Services.FileStorage;

public class LocalFileStorageService : IFileStorageService
{
    private readonly IHostEnvironment _env;
    private readonly LocalStorageOptions _options;

    public LocalFileStorageService(IHostEnvironment env, IOptions<FileStorageOptions> options)
    {
        _env = env;
        _options = options.Value.Local;
    }

    public async Task<string> UploadAsync(
        Stream stream, string fileName, string contentType, CancellationToken ct = default)
    {
        var folder = Path.Combine(_env.ContentRootPath, "wwwroot", _options.RootFolder);
        Directory.CreateDirectory(folder);

        var safeName = $"{Guid.NewGuid():N}_{Path.GetFileName(fileName)}";
        var fullPath = Path.Combine(folder, safeName);

        await using var fs = File.Create(fullPath);
        await stream.CopyToAsync(fs, ct);

        return $"/{_options.RootFolder}/{safeName}";
    }

    public Task DeleteAsync(string fileUrl, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(fileUrl)) return Task.CompletedTask;

        // فقط فایل‌های خودمان (نه URL خارجی)
        if (fileUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return Task.CompletedTask;

        var rel = fileUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var full = Path.Combine(_env.ContentRootPath, "wwwroot", rel);

        if (File.Exists(full)) File.Delete(full);
        return Task.CompletedTask;
    }

    public string GetPublicUrl(string fileUrl)
    {
        if (string.IsNullOrWhiteSpace(fileUrl)) return string.Empty;
        if (fileUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return fileUrl;

        var baseUrl = _options.PublicBaseUrl.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl)) return fileUrl;
        return $"{baseUrl}/{fileUrl.TrimStart('/')}";
    }
}