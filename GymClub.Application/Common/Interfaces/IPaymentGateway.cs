namespace GymClub.Application.Common.Interfaces;

public interface IPaymentGateway
{
    Task<PaymentRequestResult> RequestAsync(
        decimal amount, string description, string? mobile = null, string? email = null,
        CancellationToken ct = default);

    Task<PaymentVerifyResult> VerifyAsync(string authority, decimal amount, CancellationToken ct = default);
}

public record PaymentRequestResult(bool Success, string? Authority, string? PaymentUrl, string? Error);
public record PaymentVerifyResult(bool Success, string? RefId, string? Error);