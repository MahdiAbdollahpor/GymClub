namespace GymClub.Domain.Enums;

public enum OrderStatus
{
    PendingPayment = 0,   // منتظر پرداخت
    Paid = 1,             // پرداخت شده، منتظر تأیید ادمین
    Confirmed = 2,        // تأیید شده توسط ادمین
    Shipped = 3,          // ارسال شده + کد رهگیری
    Cancelled = 4         // لغو شده
}