using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GymClub.Infrastructure.Services.Payments;

public class ZarinPalPaymentGateway : IPaymentGateway
{
    private readonly HttpClient _http;
    private readonly ILogger<ZarinPalPaymentGateway> _logger;

    // 🔧 مقادیر Hardcode برای تست
    private const string MERCHANT_ID = "35ab132c-e623-49d3-896a-3442b1c6561c";
    private const bool IS_SANDBOX = true;
    private const string CALLBACK_URL = "https://localhost:44304/payment/callback";

    public ZarinPalPaymentGateway(
        HttpClient http,
        ILogger<ZarinPalPaymentGateway> logger)
    {
        _http = http;
        _logger = logger;

        var baseUri = IS_SANDBOX
            ? "https://sandbox.zarinpal.com/"
            : "https://api.zarinpal.com/";

        _http.BaseAddress = new Uri(baseUri);
        _http.DefaultRequestHeaders.Add("Accept", "application/json");

        _logger.LogInformation("ZarinPal [HARDCODED] MerchantId={MerchantId}, Sandbox={Sandbox}, Callback={Callback}",
            MERCHANT_ID, IS_SANDBOX, CALLBACK_URL);
    }

    public async Task<PaymentRequestResult> RequestAsync(
        decimal amount, string description, string? mobile = null, string? email = null,
        CancellationToken ct = default)
    {
        try
        {
            var amountInRial = (long)(amount * 10);

            var requestData = new
            {
                merchant_id = MERCHANT_ID,
                amount = amountInRial,
                callback_url = CALLBACK_URL,
                description,
                metadata = new
                {
                    mobile = mobile ?? "",
                    email = email ?? ""
                }
            };

            var json = JsonSerializer.Serialize(requestData);
            _logger.LogInformation("ZarinPal Request payload: {Json}", json);

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync("pg/v4/payment/request.json", content, ct);
            var responseString = await response.Content.ReadAsStringAsync(ct);

            _logger.LogInformation("ZarinPal Response: {Response}", responseString);

            if (!response.IsSuccessStatusCode)
            {
                return new PaymentRequestResult(false, null, null,
                    $"HTTP {response.StatusCode}");
            }

            var result = JsonSerializer.Deserialize<ZarinPalResponse>(responseString);

            if (result?.Data?.Code == 100 && !string.IsNullOrWhiteSpace(result.Data.Authority))
            {
                var payUrl = IS_SANDBOX
                    ? $"https://sandbox.zarinpal.com/pg/StartPay/{result.Data.Authority}"
                    : $"https://www.zarinpal.com/pg/StartPay/{result.Data.Authority}";

                return new PaymentRequestResult(true, result.Data.Authority, payUrl, null);
            }

            var errorMsg = result?.Errors?.FirstOrDefault()?.Message
                        ?? result?.Data?.Message
                        ?? "خطای نامشخص";
            return new PaymentRequestResult(false, null, null, errorMsg);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ZarinPal request exception");
            return new PaymentRequestResult(false, null, null, "خطا در ارتباط با درگاه");
        }
    }

    public async Task<PaymentVerifyResult> VerifyAsync(string authority, decimal amount, CancellationToken ct = default)
    {
        try
        {
            var amountInRial = (long)(amount * 10);

            var requestData = new
            {
                merchant_id = MERCHANT_ID,
                amount = amountInRial,
                authority
            };

            var content = new StringContent(
                JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json");

            var response = await _http.PostAsync("pg/v4/payment/verify.json", content, ct);
            var responseString = await response.Content.ReadAsStringAsync(ct);

            _logger.LogInformation("ZarinPal Verify: {Response}", responseString);

            if (!response.IsSuccessStatusCode)
                return new PaymentVerifyResult(false, null, $"HTTP {response.StatusCode}");

            var result = JsonSerializer.Deserialize<ZarinPalResponse>(responseString);

            if (result?.Data?.Code == 100 || result?.Data?.Code == 101)
                return new PaymentVerifyResult(true, result.Data.RefId?.ToString(), null);

            return new PaymentVerifyResult(false, null,
                result?.Errors?.FirstOrDefault()?.Message ?? "تأیید نشد");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ZarinPal verify exception");
            return new PaymentVerifyResult(false, null, "خطا در تأیید");
        }
    }
}

internal class ZarinPalResponse
{
    [JsonPropertyName("data")] public ZarinPalData? Data { get; set; }
    [JsonPropertyName("errors")] public List<ZarinPalError>? Errors { get; set; }
}

internal class ZarinPalData
{
    [JsonPropertyName("code")] public int Code { get; set; }
    [JsonPropertyName("message")] public string? Message { get; set; }
    [JsonPropertyName("authority")] public string? Authority { get; set; }
    [JsonPropertyName("ref_id")] public long? RefId { get; set; }
}

internal class ZarinPalError
{
    [JsonPropertyName("code")] public int Code { get; set; }
    [JsonPropertyName("message")] public string? Message { get; set; }
}