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
        IApplicationDbContext db, IPaymentGateway payment, IDateTimeProvider clock)
    {
        _db = db; _payment = payment; _clock = clock;
    }

    public async Task<Result<WorkoutVerifyResult>> Handle(VerifyWorkoutPaymentCommand req, CancellationToken ct)
    {
        var tx = await _db.WorkoutPaymentTransactions
            .Include(x => x.WorkoutRequest)
            .FirstOrDefaultAsync(x => x.Authority == req.Authority, ct);
        if (tx is null) return Result<WorkoutVerifyResult>.Fail("تراکنش یافت نشد");

        if (tx.Status == PaymentStatus.Verified)
            return Result<WorkoutVerifyResult>.Ok(new WorkoutVerifyResult
            { RequestId = tx.WorkoutRequestId, Success = true, Message = "قبلاً تأیید شده" });

        if (!req.Status.Equals("OK", StringComparison.OrdinalIgnoreCase))
        {
            tx.Status = PaymentStatus.Cancelled;
            tx.GatewayMessage = "کاربر لغو کرد";
            await _db.SaveChangesAsync(ct);
            return Result<WorkoutVerifyResult>.Ok(new WorkoutVerifyResult
            { RequestId = tx.WorkoutRequestId, Success = false, Message = "پرداخت لغو شد" });
        }

        var v = await _payment.VerifyAsync(req.Authority, tx.Amount, ct);
        if (!v.Success)
        {
            tx.Status = PaymentStatus.Failed;
            tx.GatewayMessage = v.Error;
            await _db.SaveChangesAsync(ct);
            return Result<WorkoutVerifyResult>.Ok(new WorkoutVerifyResult
            { RequestId = tx.WorkoutRequestId, Success = false, Message = v.Error });
        }

        var now = _clock.UtcNow;
        tx.Status = PaymentStatus.Verified;
        tx.RefId = v.RefId;
        tx.VerifiedAt = now;

        tx.WorkoutRequest.Status = WorkoutRequestStatus.Paid;
        tx.WorkoutRequest.PaidAt = now;

        await _db.SaveChangesAsync(ct);
        return Result<WorkoutVerifyResult>.Ok(new WorkoutVerifyResult
        { RequestId = tx.WorkoutRequestId, Success = true, Message = "پرداخت موفق" });
    }
}