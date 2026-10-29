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
        IApplicationDbContext db, ICurrentUserService current, IPaymentGateway payment)
    {
        _db = db; _current = current; _payment = payment;
    }

    public async Task<Result<WorkoutPaymentResult>> Handle(CreateWorkoutRequestCommand req, CancellationToken ct)
    {
        if (_current.UserId is null) return Result<WorkoutPaymentResult>.Fail("ابتدا وارد شوید");
        var userId = _current.UserId.Value;

        var active = await _db.WorkoutRequests
            .AnyAsync(x => x.UserId == userId
                        && x.Status != WorkoutRequestStatus.Cancelled
                        && x.Status != WorkoutRequestStatus.Completed, ct);
        if (active) return Result<WorkoutPaymentResult>.Fail("شما یک درخواست فعال دارید");

        var q = await _db.WorkoutQuestionnaires
            .Where(x => !x.IsDeleted && x.IsActive)
            .OrderByDescending(x => x.Version)
            .FirstOrDefaultAsync(ct);
        if (q is null) return Result<WorkoutPaymentResult>.Fail("پرسشنامه فعالی وجود ندارد");

        var request = new WorkoutRequest
        {
            UserId = userId,
            QuestionnaireId = q.Id,
            Status = WorkoutRequestStatus.PendingPayment,
            Amount = q.Price
        };
        _db.WorkoutRequests.Add(request);
        await _db.SaveChangesAsync(ct);

        var pay = await _payment.RequestAsync(q.Price, $"برنامه ورزشی — {q.Title}", null, null, ct);
        if (!pay.Success)
            return Result<WorkoutPaymentResult>.Fail($"خطا در ایجاد تراکنش: {pay.Error}");

        _db.WorkoutPaymentTransactions.Add(new WorkoutPaymentTransaction
        {
            WorkoutRequestId = request.Id,
            Authority = pay.Authority!,
            Amount = q.Price,
            Status = PaymentStatus.Initiated
        });
        await _db.SaveChangesAsync(ct);

        return Result<WorkoutPaymentResult>.Ok(new WorkoutPaymentResult
        {
            RequestId = request.Id,
            PaymentUrl = pay.PaymentUrl!
        });
    }
}