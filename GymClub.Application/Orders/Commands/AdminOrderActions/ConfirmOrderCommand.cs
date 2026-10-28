using FluentValidation;
using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using GymClub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Orders.Commands.AdminOrderActions;

// تأیید سفارش
public record ConfirmOrderCommand(Guid OrderId) : IRequest<Result>;
public class ConfirmOrderHandler : IRequestHandler<ConfirmOrderCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;
    public ConfirmOrderHandler(IApplicationDbContext db, IDateTimeProvider clock) { _db = db; _clock = clock; }
    public async Task<Result> Handle(ConfirmOrderCommand req, CancellationToken ct)
    {
        var o = await _db.Orders.FirstOrDefaultAsync(x => x.Id == req.OrderId, ct);
        if (o is null) return Result.Fail("سفارش یافت نشد");
        if (o.Status != OrderStatus.Paid) return Result.Fail("فقط سفارش پرداخت‌شده قابل تأیید است");
        o.Status = OrderStatus.Confirmed;
        o.ConfirmedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}

// ارسال سفارش
public record ShipOrderCommand(Guid OrderId, string TrackingCode) : IRequest<Result>;
public class ShipOrderValidator : AbstractValidator<ShipOrderCommand>
{
    public ShipOrderValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.TrackingCode).NotEmpty().MaximumLength(100);
    }
}
public class ShipOrderHandler : IRequestHandler<ShipOrderCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;
    public ShipOrderHandler(IApplicationDbContext db, IDateTimeProvider clock) { _db = db; _clock = clock; }
    public async Task<Result> Handle(ShipOrderCommand req, CancellationToken ct)
    {
        var o = await _db.Orders.FirstOrDefaultAsync(x => x.Id == req.OrderId, ct);
        if (o is null) return Result.Fail("سفارش یافت نشد");
        if (o.Status != OrderStatus.Confirmed) return Result.Fail("ابتدا سفارش را تأیید کنید");
        o.Status = OrderStatus.Shipped;
        o.TrackingCode = req.TrackingCode.Trim();
        o.ShippedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}

// لغو سفارش
public record CancelOrderCommand(Guid OrderId, string Reason) : IRequest<Result>;
public class CancelOrderValidator : AbstractValidator<CancelOrderCommand>
{
    public CancelOrderValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}
public class CancelOrderHandler : IRequestHandler<CancelOrderCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;
    public CancelOrderHandler(IApplicationDbContext db, IDateTimeProvider clock) { _db = db; _clock = clock; }
    public async Task<Result> Handle(CancelOrderCommand req, CancellationToken ct)
    {
        var o = await _db.Orders.Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == req.OrderId, ct);
        if (o is null) return Result.Fail("سفارش یافت نشد");
        if (o.Status == OrderStatus.Shipped || o.Status == OrderStatus.Cancelled)
            return Result.Fail("این سفارش قابل لغو نیست");

        var wasPaid = o.Status == OrderStatus.Paid || o.Status == OrderStatus.Confirmed;

        // بازگشت موجودی
        if (wasPaid)
        {
            foreach (var item in o.Items)
            {
                var p = await _db.Products.FirstOrDefaultAsync(x => x.Id == item.ProductId, ct);
                if (p is not null) p.Stock += item.Quantity;
            }
        }

        o.Status = OrderStatus.Cancelled;
        o.CancelReason = req.Reason.Trim();
        o.CancelledAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}