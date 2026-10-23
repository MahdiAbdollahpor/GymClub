using GymClub.Application.Common.Models;
using GymClub.Domain.Enums;

namespace GymClub.Application.Common.Interfaces;

public interface IOtpService
{
    Task<Result<string>> GenerateAsync(string phoneNumber, OtpPurpose purpose, CancellationToken ct = default);
    Task<Result> VerifyAsync(string phoneNumber, string code, OtpPurpose purpose, CancellationToken ct = default);
}