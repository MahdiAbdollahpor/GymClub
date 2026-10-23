using System.Security.Cryptography;
using System.Text;
using GymClub.Application.Common.Interfaces;

namespace GymClub.Infrastructure.Services;

public class OtpHasher : IOtpHasher
{
    public string Hash(string phoneNumber, string code)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes($"{phoneNumber}:{code}");
        return Convert.ToHexString(sha.ComputeHash(bytes));
    }
}