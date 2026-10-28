using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using GymClub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Orders.Commands.VerifyPayment;

public record VerifyPaymentCommand(string Authority, string Status)
    : IRequest<Result<VerifyPaymentResult>>;

public class VerifyPaymentResult
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? Message { get; set; }
}

public class VerifyPaymentCommandHandler
    : IRequestHandler<VerifyPaymentCommand, Result<VerifyPaymentResult>>
{
    private readonly IApplicationDbContext _db;
    private readonly IPaymentGateway _payment;
    private readonly IDateTimeProvider _clock;

    public VerifyPaymentCommandHandler(IApplicationDbContext db, IPaymentGateway payment, IDateTimeProvider clock)
    {
        _db = db; _payment = payment; _clock = clock;
    }

    public async Task<Result<VerifyPaymentResult>> Handle(VerifyPaymentCommand req, CancellationToken ct)
    {
        var tx = await _db.PaymentTransactions
            .Include(t => t.Order).ThenInclude(o => o.Items)
            .FirstOrDefaultAsync(t => t.Authority == req.Authority, ct);
        if (tx is null) return Result<VerifyPaymentResult>.Fail("تراکنش یافت نشد");

        var order = tx.Order;

        // قبلاً تأیید شده → idempotent
        if (tx.Status == PaymentStatus.Verified)
            return Result<VerifyPaymentResult>.Ok(new VerifyPaymentResult
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                Success = true,
                Message = "پرداخت قبلاً تأیید شده است"
            });

        if (!req.Status.Equals("OK", StringComparison.OrdinalIgnoreCase))
        {
            tx.Status = PaymentStatus.Cancelled;
            tx.GatewayMessage = "کاربر از پرداخت انصراف داد";
            await _db.SaveChangesAsync(ct);
            return Result<VerifyPaymentResult>.Ok(new VerifyPaymentResult
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                Success = false,
                Message = "پرداخت لغو شد"
            });
        }

        var verify = await _payment.VerifyAsync(req.Authority, tx.Amount, ct);
        if (!verify.Success)
        {
            tx.Status = PaymentStatus.Failed;
            tx.GatewayMessage = verify.Error;
            await _db.SaveChangesAsync(ct);
            return Result<VerifyPaymentResult>.Ok(new VerifyPaymentResult
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                Success = false,
                Message = verify.Error ?? "پرداخت تأیید نشد"
            });
        }

        var now = _clock.UtcNow;
        tx.Status = PaymentStatus.Verified;
        tx.RefId = verify.RefId;
        tx.VerifiedAt = now;

        order.Status = OrderStatus.Paid;
        order.PaidAt = now;

        // کاهش موجودی
        foreach (var item in order.Items)
        {
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == item.ProductId, ct);
            if (product is not null)
            {
                product.Stock -= item.Quantity;
                if (product.Stock < 0) product.Stock = 0;
            }
        }

        // افزایش استفاده از کد تخفیف
        if (order.DiscountCodeId.HasValue)
        {
            var dc = await _db.DiscountCodes.FirstOrDefaultAsync(d => d.Id == order.DiscountCodeId, ct);
            if (dc is not null) dc.UsedCount++;
        }

        // خالی کردن سبد
        var cartItems = await _db.CartItems
            .Where(i => i.Cart.UserId == order.UserId)
            .ToListAsync(ct);
        _db.CartItems.RemoveRange(cartItems);

        await _db.SaveChangesAsync(ct);

        return Result<VerifyPaymentResult>.Ok(new VerifyPaymentResult
        {
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            Success = true,
            Message = "پرداخت با موفقیت انجام شد"
        });
    }
}