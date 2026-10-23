using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using GymClub.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GymClub.Infrastructure.Services.FileStorage;

public class RemoteFileStorageService : IFileStorageService
{
    private readonly HttpClient _http;
    private readonly RemoteStorageOptions _options;
    private readonly ILogger<RemoteFileStorageService> _logger;

    public RemoteFileStorageService(
        HttpClient http,
        IOptions<FileStorageOptions> options,
        ILogger<RemoteFileStorageService> logger)
    {
        _http = http;
        _options = options.Value.Remote;
        _logger = logger;

        if (_options.TimeoutSeconds > 0)
            _http.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
    }

    public async Task<string> UploadAsync(
        Stream stream, string fileName, string contentType, CancellationToken ct = default)
    {
        if (!_options.Enabled)
            throw new InvalidOperationException(
                "RemoteFileStorage غیرفعال است. در appsettings.json مقدار Enabled را true کنید.");

        if (string.IsNullOrWhiteSpace(_options.UploadEndpoint))
            throw new InvalidOperationException("RemoteFileStorage:UploadEndpoint خالی است.");

        using var content = new MultipartFormDataContent();
        using var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "file", fileName);

        using var req = new HttpRequestMessage(HttpMethod.Post, _options.UploadEndpoint);
        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
            req.Headers.Add("X-Api-Key", _options.ApiKey);
        req.Content = content;

        var response = await _http.SendAsync(req, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content
            .ReadFromJsonAsync<RemoteUploadResult>(cancellationToken: ct);

        if (string.IsNullOrWhiteSpace(result?.Url))
            throw new InvalidOperationException("پاسخ هاست فایل نامعتبر است.");

        _logger.LogInformation("File uploaded to remote: {Url}", result.Url);
        return result.Url;
    }

    public async Task DeleteAsync(string fileUrl, CancellationToken ct = default)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(fileUrl)) return;

        // اگر DeleteEndpoint خالی بود، فعلاً نادیده بگیر (بعداً با مستندات هاست پر می‌شود)
        if (string.IsNullOrWhiteSpace(_options.DeleteEndpoint))
        {
            _logger.LogInformation("Remote delete requested (endpoint not configured): {Url}", fileUrl);
            return;
        }

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Delete,
                $"{_options.DeleteEndpoint.TrimEnd('/')}?url={Uri.EscapeDataString(fileUrl)}");
            if (!string.IsNullOrWhiteSpace(_options.ApiKey))
                req.Headers.Add("X-Api-Key", _options.ApiKey);

            var resp = await _http.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode)
                _logger.LogWarning("Remote delete failed: {Status} for {Url}",
                    resp.StatusCode, fileUrl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Remote delete error for {Url}", fileUrl);
        }
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

internal class RemoteUploadResult
{
    [JsonPropertyName("url")] public string? Url { get; set; }
    [JsonPropertyName("filename")] public string? Filename { get; set; }
    [JsonPropertyName("size")] public long? Size { get; set; }
}