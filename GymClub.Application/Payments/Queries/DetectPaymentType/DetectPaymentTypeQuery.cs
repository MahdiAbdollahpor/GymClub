using GymClub.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Payments.Queries.DetectPaymentType;

public enum PaymentTarget
{
    Unknown = 0,
    Order = 1,
    Workout = 2
}

public record DetectPaymentTypeQuery(string Authority) : IRequest<PaymentTarget>;

public class DetectPaymentTypeQueryHandler
    : IRequestHandler<DetectPaymentTypeQuery, PaymentTarget>
{
    private readonly IApplicationDbContext _db;
    public DetectPaymentTypeQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PaymentTarget> Handle(DetectPaymentTypeQuery req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Authority))
            return PaymentTarget.Unknown;

        var isOrder = await _db.PaymentTransactions
            .AnyAsync(t => t.Authority == req.Authority, ct);
        if (isOrder) return PaymentTarget.Order;

        var isWorkout = await _db.WorkoutPaymentTransactions
            .AnyAsync(t => t.Authority == req.Authority, ct);
        if (isWorkout) return PaymentTarget.Workout;

        return PaymentTarget.Unknown;
    }
}