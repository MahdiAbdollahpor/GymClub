namespace GymClub.Infrastructure.Services.FileStorage;

public class FileStorageOptions
{
    /// "Local" یا "Remote"
    public string Active { get; set; } = "Local";

    public LocalStorageOptions Local { get; set; } = new();
    public RemoteStorageOptions Remote { get; set; } = new();
}

public class LocalStorageOptions
{
    /// پوشه ریشه داخل wwwroot
    public string RootFolder { get; set; } = "uploads";

    /// آدرس پایه برای ساخت URL عمومی (اگر خالی باشد، "/" استفاده می‌شود)
    public string PublicBaseUrl { get; set; } = "";
}

public class RemoteStorageOptions
{
    public bool Enabled { get; set; }
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string UploadEndpoint { get; set; } = string.Empty;
    public string DeleteEndpoint { get; set; } = string.Empty;
    public string PublicBaseUrl { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 60;
}