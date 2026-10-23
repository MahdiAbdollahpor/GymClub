namespace GymClub.Application.Common.Interfaces;

public interface IFileStorageService
{
    /// آپلود فایل، برگرداندن مسیر نسبی/URL
    Task<string> UploadAsync(
        Stream stream,
        string fileName,
        string contentType,
        CancellationToken ct = default);

    /// حذف فایل
    Task DeleteAsync(string fileUrl, CancellationToken ct = default);

    /// گرفتن URL عمومی برای نمایش به کاربر
    string GetPublicUrl(string fileUrl);
}