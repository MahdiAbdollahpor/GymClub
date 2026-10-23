namespace GymClub.Application.Common.Interfaces;

public interface IImageStorageService
{
    Task<string> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken ct = default);
    Task DeleteAsync(string fileUrl, CancellationToken ct = default);
    string GetPublicUrl(string fileUrl);
}