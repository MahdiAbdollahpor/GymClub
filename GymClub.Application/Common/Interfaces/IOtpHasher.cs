namespace GymClub.Application.Common.Interfaces;

public interface IOtpHasher
{
    string Hash(string phoneNumber, string code);
}