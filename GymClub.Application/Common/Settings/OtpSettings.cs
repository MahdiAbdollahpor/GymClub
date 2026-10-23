namespace GymClub.Application.Common.Settings;

public class OtpSettings
{
    public int CodeLength { get; set; } = 5;
    public int ExpiryMinutes { get; set; } = 2;
    public int MaxAttempts { get; set; } = 5;
    public int ResendCooldownSeconds { get; set; } = 60;
}