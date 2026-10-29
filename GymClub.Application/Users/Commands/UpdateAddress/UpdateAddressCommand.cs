using FluentValidation;
using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Users.Commands.UpdateAddress;

public record UpdateAddressCommand(
    Guid Id, string Title, string Province, string City, string PostalCode,
    string FullAddress, string ReceiverName, string ReceiverPhone, bool IsDefault
) : IRequest<Result>;

public class UpdateAddressCommandValidator : AbstractValidator<UpdateAddressCommand>
{
    public UpdateAddressCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Province).NotEmpty().MaximumLength(100);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.FullAddress).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.ReceiverName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.ReceiverPhone).NotEmpty().Matches(@"^09\d{9}$");
    }
}

public class UpdateAddressCommandHandler : IRequestHandler<UpdateAddressCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _current;

    public UpdateAddressCommandHandler(IApplicationDbContext db, ICurrentUserService current)
    {
        _db = db; _current = current;
    }

    public async Task<Result> Handle(UpdateAddressCommand req, CancellationToken ct)
    {
        if (_current.UserId is null) return Result.Fail("ابتدا وارد شوید");

        var entity = await _db.Addresses
            .FirstOrDefaultAsync(a => a.Id == req.Id && a.UserId == _current.UserId.Value, ct);
        if (entity is null) return Result.Fail("آدرس یافت نشد");

        if (req.IsDefault)
        {
            var existing = await _db.Addresses
                .Where(a => a.UserId == _current.UserId.Value && a.IsDefault && a.Id != req.Id)
                .ToListAsync(ct);
            foreach (var a in existing) a.IsDefault = false;
        }

        entity.Title = req.Title.Trim();
        entity.Province = req.Province.Trim();
        entity.City = req.City.Trim();
        entity.PostalCode = req.PostalCode?.Trim() ?? "";
        entity.FullAddress = req.FullAddress.Trim();
        entity.ReceiverName = req.ReceiverName.Trim();
        entity.ReceiverPhone = req.ReceiverPhone.Trim();
        entity.IsDefault = req.IsDefault;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}