using FluentValidation;
using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using GymClub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Users.Commands.AddAddress;

public record AddAddressCommand(
    string Title,
    string Province,
    string City,
    string PostalCode,
    string FullAddress,
    string ReceiverName,
    string ReceiverPhone,
    bool IsDefault
) : IRequest<Result<Guid>>;

public class AddAddressCommandValidator : AbstractValidator<AddAddressCommand>
{
    public AddAddressCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Province).NotEmpty().MaximumLength(100);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PostalCode).MaximumLength(20);
        RuleFor(x => x.FullAddress).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.ReceiverName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.ReceiverPhone).NotEmpty().Matches(@"^09\d{9}$")
            .WithMessage("شماره تماس معتبر نیست");
    }
}

public class AddAddressCommandHandler : IRequestHandler<AddAddressCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _current;

    public AddAddressCommandHandler(IApplicationDbContext db, ICurrentUserService current)
    {
        _db = db; _current = current;
    }

    public async Task<Result<Guid>> Handle(AddAddressCommand req, CancellationToken ct)
    {
        if (_current.UserId is null) return Result<Guid>.Fail("ابتدا وارد شوید");
        var userId = _current.UserId.Value;

        // اگر اولین آدرس است، پیش‌فرض شود
        var hasAny = await _db.Addresses.AnyAsync(a => a.UserId == userId, ct);

        if (req.IsDefault || !hasAny)
        {
            var existing = await _db.Addresses
                .Where(a => a.UserId == userId && a.IsDefault).ToListAsync(ct);
            foreach (var a in existing) a.IsDefault = false;
        }

        var entity = new Address
        {
            UserId = userId,
            Title = req.Title.Trim(),
            Province = req.Province.Trim(),
            City = req.City.Trim(),
            PostalCode = req.PostalCode?.Trim() ?? "",
            FullAddress = req.FullAddress.Trim(),
            ReceiverName = req.ReceiverName.Trim(),
            ReceiverPhone = req.ReceiverPhone.Trim(),
            IsDefault = req.IsDefault || !hasAny
        };

        _db.Addresses.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Result<Guid>.Ok(entity.Id);
    }
}