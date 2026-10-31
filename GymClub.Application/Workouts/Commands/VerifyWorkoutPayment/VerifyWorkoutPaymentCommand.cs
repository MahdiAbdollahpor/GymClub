using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using GymClub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Workouts.Commands.VerifyWorkoutPayment;

public record VerifyWorkoutPaymentCommand(string Authority, string Status)
    : IRequest<Result<WorkoutVerifyResult>>;

public class WorkoutVerifyResult
{
    public Guid RequestId { get; set; }
    public bool Success { get; set; }
    public string? Message { get; set; }
}

public class VerifyWorkoutPaymentCommandHandler
    : IRequestHandler<VerifyWorkoutPaymentCommand, Result<WorkoutVerifyResult>>
{
    private readonly IApplicationDbContext _db;
    private readonly IPaymentGateway _payment;
    private readonly IDateTimeProvider _clock;

    public VerifyWorkoutPaymentCommandHandler(
        IApplicationDbContext db,
        IPaymentGateway payment,
        IDateTimeProvider clock)
    {
        _db = db;
        _payment = payment;
        _clock = clock;
    }

    public async Task<Result<WorkoutVerifyResult>> Handle(
        VerifyWorkoutPaymentCommand req, CancellationToken ct)
    {
        // ─── ۱. یافتن تراکنش ───
        var tx = await _db.WorkoutPaymentTransactions
            .Include(x => x.WorkoutRequest)
            .FirstOrDefaultAsync(x => x.Authority == req.Authority, ct);

        if (tx is null)
            return Result<WorkoutVerifyResult>.Fail("تراکنش یافت نشد");

        var request = tx.WorkoutRequest;

        // ─── ۲. اگر قبلاً تأیید شده (Idempotent) ───
        if (tx.Status == PaymentStatus.Verified)
        {
            return Result<WorkoutVerifyResult>.Ok(new WorkoutVerifyResult
            {
                RequestId = request.Id,
                Success = true,
                Message = "پرداخت قبلاً تأیید شده است"
            });
        }

        // ─── ۳. اگر درگاه لغو کرده (Status != OK) ───
        if (!req.Status.Equals("OK", StringComparison.OrdinalIgnoreCase))
        {
            tx.Status = PaymentStatus.Cancelled;
            tx.GatewayMessage = "کاربر از پرداخت انصراف داد";

            // درخواست را لغو کن تا کاربر بتواند دوباره تلاش کند
            if (request.Status == WorkoutRequestStatus.PendingPayment)
                request.Status = WorkoutRequestStatus.Cancelled;

            await _db.SaveChangesAsync(ct);

            return Result<WorkoutVerifyResult>.Ok(new WorkoutVerifyResult
            {
                RequestId = request.Id,
                Success = false,
                Message = "پرداخت لغو شد"
            });
        }

        // ─── ۴. تأیید از زرین‌پال ───
        var verify = await _payment.VerifyAsync(req.Authority, tx.Amount, ct);

        if (!verify.Success)
        {
            tx.Status = PaymentStatus.Failed;
            tx.GatewayMessage = verify.Error;

            // درخواست را لغو کن
            if (request.Status == WorkoutRequestStatus.PendingPayment)
                request.Status = WorkoutRequestStatus.Cancelled;

            await _db.SaveChangesAsync(ct);

            return Result<WorkoutVerifyResult>.Ok(new WorkoutVerifyResult
            {
                RequestId = request.Id,
                Success = false,
                Message = verify.Error ?? "پرداخت تأیید نشد"
            });
        }

        // ─── ۵. پرداخت موفق ───
        var now = _clock.UtcNow;

        tx.Status = PaymentStatus.Verified;
        tx.RefId = verify.RefId;
        tx.VerifiedAt = now;

        request.Status = WorkoutRequestStatus.Paid;
        request.PaidAt = now;

        await _db.SaveChangesAsync(ct);

        return Result<WorkoutVerifyResult>.Ok(new WorkoutVerifyResult
        {
            RequestId = request.Id,
            Success = true,
            Message = "پرداخت با موفقیت انجام شد"
        });
    }
}