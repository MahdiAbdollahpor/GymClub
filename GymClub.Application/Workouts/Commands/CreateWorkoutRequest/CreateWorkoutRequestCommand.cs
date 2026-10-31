using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using GymClub.Domain.Entities;
using GymClub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Workouts.Commands.CreateWorkoutRequest;

public record CreateWorkoutRequestCommand : IRequest<Result<WorkoutPaymentResult>>;

public class WorkoutPaymentResult
{
    public Guid RequestId { get; set; }
    public string PaymentUrl { get; set; } = string.Empty;
}

public class CreateWorkoutRequestCommandHandler
    : IRequestHandler<CreateWorkoutRequestCommand, Result<WorkoutPaymentResult>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _current;
    private readonly IPaymentGateway _payment;

    public CreateWorkoutRequestCommandHandler(
        IApplicationDbContext db,
        ICurrentUserService current,
        IPaymentGateway payment)
    {
        _db = db;
        _current = current;
        _payment = payment;
    }

    public async Task<Result<WorkoutPaymentResult>> Handle(
        CreateWorkoutRequestCommand req, CancellationToken ct)
    {
        if (_current.UserId is null)
            return Result<WorkoutPaymentResult>.Fail("ابتدا وارد شوید");

        var userId = _current.UserId.Value;

        // ─── ۱. لغو درخواست‌های PendingPayment قبلی (پرداخت ناتمام) ───
        var stalePending = await _db.WorkoutRequests
            .Where(x => x.UserId == userId
                     && x.Status == WorkoutRequestStatus.PendingPayment)
            .ToListAsync(ct);

        foreach (var stale in stalePending)
        {
            stale.Status = WorkoutRequestStatus.Cancelled;
            stale.AdminNote = "پرداخت ناتمام — لغو خودکار";

            // تراکنش‌های Initiated مرتبط را هم لغو کن
            var staleTx = await _db.WorkoutPaymentTransactions
                .Where(t => t.WorkoutRequestId == stale.Id
                         && t.Status == PaymentStatus.Initiated)
                .ToListAsync(ct);

            foreach (var tx in staleTx)
            {
                tx.Status = PaymentStatus.Cancelled;
                tx.GatewayMessage = "لغو خودکار به دلیل پرداخت ناتمام";
            }
        }

        if (stalePending.Any())
            await _db.SaveChangesAsync(ct);

        // ─── ۲. بررسی درخواست فعال (Paid / AnswersSubmitted / Completed) ───
        var active = await _db.WorkoutRequests
            .AnyAsync(x => x.UserId == userId
                        && (x.Status == WorkoutRequestStatus.Paid
                         || x.Status == WorkoutRequestStatus.AnswersSubmitted
                         || x.Status == WorkoutRequestStatus.Completed), ct);

        if (active)
            return Result<WorkoutPaymentResult>.Fail("شما یک درخواست فعال دارید");

        // ─── ۳. یافتن پرسشنامه فعال ───
        var questionnaire = await _db.WorkoutQuestionnaires
            .Where(x => !x.IsDeleted && x.IsActive)
            .OrderByDescending(x => x.Version)
            .FirstOrDefaultAsync(ct);

        if (questionnaire is null)
            return Result<WorkoutPaymentResult>.Fail("پرسشنامه فعالی وجود ندارد");

        // ─── ۴. ایجاد درخواست ───
        var request = new WorkoutRequest
        {
            UserId = userId,
            QuestionnaireId = questionnaire.Id,
            Status = WorkoutRequestStatus.PendingPayment,
            Amount = questionnaire.Price
        };
        _db.WorkoutRequests.Add(request);
        await _db.SaveChangesAsync(ct);

        // ─── ۵. درخواست پرداخت از زرین‌پال ───
        var pay = await _payment.RequestAsync(
    questionnaire.Price,
    $"برنامه ورزشی — {questionnaire.Title}",
    _current.PhoneNumber,   // ← شماره موبایل کاربر
    null,
    ct);

        if (!pay.Success)
        {
            // اگر ایجاد تراکنش ناموفق بود، درخواست را لغو کن
            request.Status = WorkoutRequestStatus.Cancelled;
            request.AdminNote = $"خطای درگاه: {pay.Error}";
            await _db.SaveChangesAsync(ct);

            return Result<WorkoutPaymentResult>.Fail(
                $"خطا در ایجاد تراکنش: {pay.Error}");
        }

        // ─── ۶. ثبت تراکنش ───
        _db.WorkoutPaymentTransactions.Add(new WorkoutPaymentTransaction
        {
            WorkoutRequestId = request.Id,
            Authority = pay.Authority!,
            Amount = questionnaire.Price,
            Status = PaymentStatus.Initiated,
            Gateway = "ZarinPal"
        });
        await _db.SaveChangesAsync(ct);

        return Result<WorkoutPaymentResult>.Ok(new WorkoutPaymentResult
        {
            RequestId = request.Id,
            PaymentUrl = pay.PaymentUrl!
        });
    }
}