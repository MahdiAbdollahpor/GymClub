using GymClub.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace GymClub.Infrastructure.Services.FileStorage;

/// آپلود عکس — همیشه لوکال
public class ImageStorageService : IImageStorageService
{
    private readonly LocalFileStorageService _local;
    private readonly LocalStorageOptions _opts;

    public ImageStorageService(LocalFileStorageService local, IOptions<FileStorageOptions> opts)
    {
        _local = local;
        _opts = opts.Value.Local;
    }

    public Task<string> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken ct = default)
        => _local.UploadAsync(stream, fileName, contentType, ct);

    public Task DeleteAsync(string fileUrl, CancellationToken ct = default)
        => _local.DeleteAsync(fileUrl, ct);

    public string GetPublicUrl(string fileUrl) => _local.GetPublicUrl(fileUrl);
}