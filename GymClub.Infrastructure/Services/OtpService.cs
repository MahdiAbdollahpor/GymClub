using System.Security.Cryptography;
using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using GymClub.Application.Common.Settings;
using GymClub.Domain.Entities;
using GymClub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GymClub.Infrastructure.Services;

public class OtpService : IOtpService
{
    private readonly IApplicationDbContext _db;
    private readonly IOtpHasher _hasher;
    private readonly IDateTimeProvider _clock;
    private readonly OtpSettings _settings;

    public OtpService(
        IApplicationDbContext db,
        IOtpHasher hasher,
        IDateTimeProvider clock,
        IOptions<OtpSettings> settings)
    {
        _db = db;
        _hasher = hasher;
        _clock = clock;
        _settings = settings.Value;
    }

    public async Task<Result<string>> GenerateAsync(string phoneNumber, OtpPurpose purpose, CancellationToken ct = default)
    {
        var now = _clock.UtcNow;

        // Cooldown
        var last = await _db.OtpCodes
            .Where(x => x.PhoneNumber == phoneNumber && x.Purpose == purpose)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (last != null && (now - last.CreatedAt).TotalSeconds < _settings.ResendCooldownSeconds)
        {
            var wait = _settings.ResendCooldownSeconds - (int)(now - last.CreatedAt).TotalSeconds;
            return Result<string>.Fail($"لطفاً {wait} ثانیه دیگر تلاش کنید");
        }

        // کد جدید
        var code = GenerateCode(_settings.CodeLength);
        var entity = new OtpCode
        {
            PhoneNumber = phoneNumber,
            CodeHash = _hasher.Hash(phoneNumber, code),
            Purpose = purpose,
            ExpiresAt = now.AddMinutes(_settings.ExpiryMinutes),
        };
        _db.OtpCodes.Add(entity);
        await _db.SaveChangesAsync(ct);

        return Result<string>.Ok(code);
    }

    public async Task<Result> VerifyAsync(string phoneNumber, string code, OtpPurpose purpose, CancellationToken ct = default)
    {
        var now = _clock.UtcNow;

        var otp = await _db.OtpCodes
            .Where(x => x.PhoneNumber == phoneNumber
                     && x.Purpose == purpose
                     && !x.IsUsed)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (otp is null) return Result.Fail("کد یافت نشد، لطفاً دوباره درخواست دهید");
        if (otp.ExpiresAt < now) return Result.Fail("کد منقضی شده است");
        if (otp.AttemptCount >= _settings.MaxAttempts) return Result.Fail("تعداد تلاش‌ها بیش از حد مجاز");

        if (otp.CodeHash != _hasher.Hash(phoneNumber, code))
        {
            otp.AttemptCount++;
            await _db.SaveChangesAsync(ct);
            return Result.Fail("کد وارد شده صحیح نیست");
        }

        otp.IsUsed = true;
        otp.UsedAt = now;
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    private static string GenerateCode(int length)
    {
        var max = (int)Math.Pow(10, length);
        return RandomNumberGenerator.GetInt32(0, max).ToString(new string('0', length));
    }
}