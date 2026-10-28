using System.Net.Http.Json;
using System.Text.Json.Serialization;
using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GymClub.Infrastructure.Services.Payments;

public class ZarinPalPaymentGateway : IPaymentGateway
{
    private readonly HttpClient _http;
    private readonly ZarinPalSettings _settings;
    private readonly ILogger<ZarinPalPaymentGateway> _logger;

    public ZarinPalPaymentGateway(
        HttpClient http,
        IOptions<ZarinPalSettings> settings,
        ILogger<ZarinPalPaymentGateway> logger)
    {
        _http = http;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<PaymentRequestResult> RequestAsync(
        decimal amount, string description, string? mobile = null, string? email = null,
        CancellationToken ct = default)
    {
        try
        {
            var payload = new
            {
                merchant_id = _settings.MerchantId,
                amount = (long)amount,   // ریال
                description,
                callback_url = _settings.CallbackUrl,
                metadata = new { mobile = mobile ?? "", email = email ?? "" }
            };

            var resp = await _http.PostAsJsonAsync(
                $"{_settings.BaseUrl.TrimEnd('/')}/request.json", payload, ct);

            var result = await resp.Content.ReadFromJsonAsync<ZarinPalRequestResponse>(cancellationToken: ct);

            if (result?.Data?.Code == 100 && !string.IsNullOrWhiteSpace(result.Data.Authority))
            {
                var url = string.Format(_settings.StartPayBaseUrl, result.Data.Authority);
                return new PaymentRequestResult(true, result.Data.Authority, url, null);
            }

            var err = result?.Errors?.FirstOrDefault()?.Message ?? "خطای نامشخص در ایجاد تراکنش";
            _logger.LogWarning("ZarinPal request failed: {Error}", err);
            return new PaymentRequestResult(false, null, null, err);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ZarinPal request exception");
            return new PaymentRequestResult(false, null, null, "خطا در ارتباط با درگاه پرداخت");
        }
    }

    public async Task<PaymentVerifyResult> VerifyAsync(string authority, decimal amount, CancellationToken ct = default)
    {
        try
        {
            var payload = new
            {
                merchant_id = _settings.MerchantId,
                amount = (long)amount,
                authority
            };

            var resp = await _http.PostAsJsonAsync(
                $"{_settings.BaseUrl.TrimEnd('/')}/verify.json", payload, ct);

            var result = await resp.Content.ReadFromJsonAsync<ZarinPalVerifyResponse>(cancellationToken: ct);

            if (result?.Data?.Code == 100 || result?.Data?.Code == 101)
                return new PaymentVerifyResult(true, result.Data.RefId, null);

            var err = result?.Errors?.FirstOrDefault()?.Message ?? "پرداخت تأیید نشد";
            return new PaymentVerifyResult(false, null, err);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ZarinPal verify exception");
            return new PaymentVerifyResult(false, null, "خطا در تأیید پرداخت");
        }
    }
}

// ─── Response Models ───
internal class ZarinPalRequestResponse
{
    [JsonPropertyName("data")] public DataObj? Data { get; set; }
    [JsonPropertyName("errors")] public List<ErrorObj>? Errors { get; set; }
    internal class DataObj
    {
        [JsonPropertyName("code")] public int Code { get; set; }
        [JsonPropertyName("authority")] public string? Authority { get; set; }
    }
}

internal class ZarinPalVerifyResponse
{
    [JsonPropertyName("data")] public DataObj? Data { get; set; }
    [JsonPropertyName("errors")] public List<ErrorObj>? Errors { get; set; }
    internal class DataObj
    {
        [JsonPropertyName("code")] public int Code { get; set; }
        [JsonPropertyName("ref_id")] public string? RefId { get; set; }
    }
}

internal class ErrorObj
{
    [JsonPropertyName("code")] public int Code { get; set; }
    [JsonPropertyName("message")] public string? Message { get; set; }
}