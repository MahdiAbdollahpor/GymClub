using GymClub.Domain.Enums;

namespace GymClub.Application.Discounts.Dtos;

public class DiscountCodeDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public DiscountType Type { get; set; }
    public decimal Value { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public int? MaxUsageCount { get; set; }
    public int UsedCount { get; set; }
    public DateTime? StartsAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    // محاسبه‌شده
    public string TypeLabel => Type == DiscountType.Percentage
        ? $"{Value:N0}٪"
        : $"{Value:N0} تومان";

    public string StatusLabel
    {
        get
        {
            if (!IsActive) return "غیرفعال";
            var now = DateTime.UtcNow;
            if (StartsAt.HasValue && StartsAt > now) return "هنوز فعال نشده";
            if (ExpiresAt.HasValue && ExpiresAt < now) return "منقضی شده";
            if (MaxUsageCount.HasValue && UsedCount >= MaxUsageCount) return "ظرفیت پر شده";
            return "فعال";
        }
    }

    public string StatusChip => StatusLabel switch
    {
        "فعال" => "k-chip-success",
        "غیرفعال" => "k-chip-neutral",
        "منقضی شده" => "k-chip-warning",
        "هنوز فعال نشده" => "k-chip-crimson",
        "ظرفیت پر شده" => "k-chip-warning",
        _ => "k-chip-neutral"
    };

    public int? RemainingUses => MaxUsageCount.HasValue
        ? Math.Max(0, MaxUsageCount.Value - UsedCount)
        : null;
}