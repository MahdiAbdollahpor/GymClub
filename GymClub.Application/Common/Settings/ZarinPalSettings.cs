namespace GymClub.Application.Common.Settings;

public class ZarinPalSettings
{
    public string MerchantId { get; set; } = string.Empty;
    public bool IsSandbox { get; set; } = true;
    public string CallbackUrl { get; set; } = string.Empty;
    public string StartPayBaseUrl { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
}