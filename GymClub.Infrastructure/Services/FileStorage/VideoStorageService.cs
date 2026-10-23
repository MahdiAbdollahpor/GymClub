using GymClub.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace GymClub.Infrastructure.Services.FileStorage;

/// آپلود ویدیو — لوکال یا ریموت بر اساس FileStorage:Active
public class VideoStorageService : IVideoStorageService
{
    private readonly IFileStorageService _inner;

    public VideoStorageService(
        IConfiguration config,
        LocalFileStorageService local,
        RemoteFileStorageService remote)
    {
        var active = config["FileStorage:Active"] ?? "Local";
        _inner = active.Equals("Remote", StringComparison.OrdinalIgnoreCase) ? remote : local;
    }

    public Task<string> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken ct = default)
        => _inner.UploadAsync(stream, fileName, contentType, ct);

    public Task DeleteAsync(string fileUrl, CancellationToken ct = default)
        => _inner.DeleteAsync(fileUrl, ct);

    public string GetPublicUrl(string fileUrl) => _inner.GetPublicUrl(fileUrl);
}