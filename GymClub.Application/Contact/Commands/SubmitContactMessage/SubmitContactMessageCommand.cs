using FluentValidation;
using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using GymClub.Domain.Entities;
using MediatR;

namespace GymClub.Application.Contact.Commands.SubmitContactMessage;

public record SubmitContactMessageCommand(
    string FullName,
    string PhoneNumber,
    string? Email,
    string Subject,
    string Message
) : IRequest<Result<Guid>>;

public class SubmitContactMessageValidator : AbstractValidator<SubmitContactMessageCommand>
{
    public SubmitContactMessageValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.PhoneNumber).NotEmpty().Matches(@"^09\d{9}$")
            .WithMessage("شماره موبایل معتبر نیست");
        RuleFor(x => x.Email).EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Message).NotEmpty().MinimumLength(10).MaximumLength(4000);
    }
}

public class SubmitContactMessageHandler : IRequestHandler<SubmitContactMessageCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    public SubmitContactMessageHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<Guid>> Handle(SubmitContactMessageCommand req, CancellationToken ct)
    {
        var entity = new ContactMessage
        {
            FullName = req.FullName.Trim(),
            PhoneNumber = req.PhoneNumber.Trim(),
            Email = req.Email?.Trim(),
            Subject = req.Subject.Trim(),
            Message = req.Message.Trim()
        };
        _db.ContactMessages.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Result<Guid>.Ok(entity.Id);
    }
}