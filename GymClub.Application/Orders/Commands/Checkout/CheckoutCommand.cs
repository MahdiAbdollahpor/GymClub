using FluentValidation;
using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using GymClub.Domain.Entities;
using GymClub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Orders.Commands.Checkout;

public record CheckoutCommand(
    Guid AddressId,
    string? DiscountCode,
    decimal ShippingCost = 0
) : IRequest<Result<CheckoutResult>>;

public class CheckoutResult
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PaymentUrl { get; set; } = string.Empty;
}

public class CheckoutCommandValidator : AbstractValidator<CheckoutCommand>
{
    public CheckoutCommandValidator()
    {
        RuleFor(x => x.AddressId).NotEmpty();
    }
}

public class CheckoutCommandHandler : IRequestHandler<CheckoutCommand, Result<CheckoutResult>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _current;
    private readonly IPaymentGateway _payment;
    private readonly IDateTimeProvider _clock;

    public CheckoutCommandHandler(
        IApplicationDbContext db, ICurrentUserService current,
        IPaymentGateway payment, IDateTimeProvider clock)
    {
        _db = db; _current = current; _payment = payment; _clock = clock;
    }

    public async Task<Result<CheckoutResult>> Handle(CheckoutCommand req, CancellationToken ct)
    {
        if (_current.UserId is null) return Result<CheckoutResult>.Fail("ابتدا وارد شوید");
        var userId = _current.UserId.Value;

        var address = await _db.Addresses
            .FirstOrDefaultAsync(a => a.Id == req.AddressId && a.UserId == userId, ct);
        if (address is null) return Result<CheckoutResult>.Fail("آدرس یافت نشد");

        var cart = await _db.Carts
            .Include(c => c.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Images)
            .FirstOrDefaultAsync(c => c.UserId == userId, ct);
        if (cart is null || !cart.Items.Any()) return Result<CheckoutResult>.Fail("سبد خرید خالی است");

        // اعتبارسنجی موجودی
        foreach (var item in cart.Items)
        {
            if (item.Product.IsDeleted || !item.Product.IsActive)
                return Result<CheckoutResult>.Fail($"محصول «{item.Product.Name}» غیرفعال شده است");
            if (item.Quantity > item.Product.Stock)
                return Result<CheckoutResult>.Fail($"موجودی «{item.Product.Name}» کافی نیست");
        }

        decimal subTotal = cart.Items.Sum(i => i.UnitPriceSnapshot * i.Quantity);
        decimal discountAmount = 0;
        Guid? discountCodeId = null;

        if (!string.IsNullOrWhiteSpace(req.DiscountCode))
        {
            var v = await ValidateCodeAsync(req.DiscountCode, subTotal, ct);
            if (!v.Success) return Result<CheckoutResult>.Fail(v.Error!);
            discountAmount = v.Amount;
            discountCodeId = v.DiscountCodeId;
        }

        var total = subTotal - discountAmount + req.ShippingCost;
        if (total < 0) total = 0;

        var orderNumber = GenerateOrderNumber();

        var order = new Order
        {
            OrderNumber = orderNumber,
            UserId = userId,
            Status = OrderStatus.PendingPayment,
            ReceiverName = address.ReceiverName,
            ReceiverPhone = address.ReceiverPhone,
            Province = address.Province,
            City = address.City,
            PostalCode = address.PostalCode,
            FullAddress = address.FullAddress,
            SubTotal = subTotal,
            DiscountAmount = discountAmount,
            ShippingCost = req.ShippingCost,
            Total = total,
            DiscountCodeId = discountCodeId
        };

        foreach (var item in cart.Items)
        {
            var mainImg = item.Product.Images.FirstOrDefault(i => i.IsMain)?.Url
                       ?? item.Product.Images.FirstOrDefault()?.Url;
            order.Items.Add(new OrderItem
            {
                ProductId = item.ProductId,
                ProductNameSnapshot = item.Product.Name,
                ProductImageSnapshot = mainImg,
                UnitPriceSnapshot = item.UnitPriceSnapshot,
                Quantity = item.Quantity,
                LineTotal = item.UnitPriceSnapshot * item.Quantity
            });
        }

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(ct);

        // درخواست پرداخت
        var payReq = await _payment.RequestAsync(total, $"سفارش {orderNumber}",
            address.ReceiverPhone, ct: ct);
        if (!payReq.Success)
            return Result<CheckoutResult>.Fail($"خطا در ایجاد تراکنش: {payReq.Error}");

        _db.PaymentTransactions.Add(new PaymentTransaction
        {
            OrderId = order.Id,
            Authority = payReq.Authority!,
            Amount = total,
            Status = PaymentStatus.Initiated,
            Gateway = "ZarinPal"
        });
        await _db.SaveChangesAsync(ct);

        return Result<CheckoutResult>.Ok(new CheckoutResult
        {
            OrderId = order.Id,
            OrderNumber = orderNumber,
            Amount = total,
            PaymentUrl = payReq.PaymentUrl!
        });
    }

    private async Task<(bool Success, string? Error, decimal Amount, Guid? DiscountCodeId)> ValidateCodeAsync(
        string code, decimal subTotal, CancellationToken ct)
    {
        var now = _clock.UtcNow;
        var c = code.Trim().ToUpperInvariant();
        var dc = await _db.DiscountCodes
            .FirstOrDefaultAsync(d => d.Code.ToUpper() == c && !d.IsDeleted && d.IsActive, ct);
        if (dc is null) return (false, "کد تخفیف معتبر نیست", 0, null);
        if (dc.StartsAt > now) return (false, "کد هنوز فعال نشده است", 0, null);
        if (dc.ExpiresAt < now) return (false, "کد منقضی شده است", 0, null);
        if (dc.MaxUsageCount.HasValue && dc.UsedCount >= dc.MaxUsageCount)
            return (false, "ظرفیت استفاده از کد پر شده است", 0, null);
        if (dc.MinOrderAmount.HasValue && subTotal < dc.MinOrderAmount)
            return (false, $"حداقل مبلغ سفارش {dc.MinOrderAmount.Value:N0} تومان است", 0, null);

        decimal discount = dc.Type == DiscountType.Percentage
            ? subTotal * dc.Value / 100m
            : dc.Value;
        if (dc.MaxDiscountAmount.HasValue && discount > dc.MaxDiscountAmount.Value)
            discount = dc.MaxDiscountAmount.Value;
        if (discount > subTotal) discount = subTotal;

        return (true, null, discount, dc.Id);
    }

    private static string GenerateOrderNumber()
        => $"GC{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(100, 999)}";
}