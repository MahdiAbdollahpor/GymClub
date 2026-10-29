using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using GymClub.Application.Users.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Users.Queries.GetAddressById;

public record GetAddressByIdQuery(Guid Id) : IRequest<Result<AddressDto>>;

public class GetAddressByIdQueryHandler : IRequestHandler<GetAddressByIdQuery, Result<AddressDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _current;

    public GetAddressByIdQueryHandler(IApplicationDbContext db, ICurrentUserService current)
    {
        _db = db; _current = current;
    }

    public async Task<Result<AddressDto>> Handle(GetAddressByIdQuery req, CancellationToken ct)
    {
        if (_current.UserId is null) return Result<AddressDto>.Fail("ابتدا وارد شوید");

        var a = await _db.Addresses.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == req.Id && x.UserId == _current.UserId.Value, ct);
        if (a is null) return Result<AddressDto>.Fail("آدرس یافت نشد");

        return Result<AddressDto>.Ok(new AddressDto
        {
            Id = a.Id,
            Title = a.Title,
            Province = a.Province,
            City = a.City,
            PostalCode = a.PostalCode,
            FullAddress = a.FullAddress,
            ReceiverName = a.ReceiverName,
            ReceiverPhone = a.ReceiverPhone,
            IsDefault = a.IsDefault
        });
    }
}