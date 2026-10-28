using GymClub.Application.Common.Interfaces;
using GymClub.Application.Users.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Users.Queries.GetAddresses;

public record GetAddressesQuery : IRequest<List<AddressDto>>;

public class GetAddressesQueryHandler : IRequestHandler<GetAddressesQuery, List<AddressDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _current;

    public GetAddressesQueryHandler(IApplicationDbContext db, ICurrentUserService current)
    {
        _db = db; _current = current;
    }

    public async Task<List<AddressDto>> Handle(GetAddressesQuery req, CancellationToken ct)
    {
        if (_current.UserId is null) return new();

        return await _db.Addresses
            .AsNoTracking()
            .Where(a => a.UserId == _current.UserId.Value)
            .OrderByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.CreatedAt)
            .Select(a => new AddressDto
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
            })
            .ToListAsync(ct);
    }
}