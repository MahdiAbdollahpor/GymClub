using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using GymClub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Users.Queries.GetUserDetails;

public class UserDetailsDto
{
    public Guid Id { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string DisplayUserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsPhoneConfirmed { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public List<UserOrderDto> Orders { get; set; } = new();
    public List<UserWorkoutRequestDto> WorkoutRequests { get; set; } = new();
    public List<UserProgramDto> Programs { get; set; } = new();
}

public class UserOrderDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public decimal Total { get; set; }
    public int ItemCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UserWorkoutRequestDto
{
    public Guid Id { get; set; }
    public string QuestionnaireTitle { get; set; } = string.Empty;
    public WorkoutRequestStatus Status { get; set; }
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UserProgramDto
{
    public Guid Id { get; set; }
    public Guid RequestId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int DaysCount { get; set; }
    public bool IsVisibleToUser { get; set; }
    public DateTime CreatedAt { get; set; }
}

public record GetUserDetailsQuery(Guid Id) : IRequest<Result<UserDetailsDto>>;

public class GetUserDetailsQueryHandler
    : IRequestHandler<GetUserDetailsQuery, Result<UserDetailsDto>>
{
    private readonly IApplicationDbContext _db;
    public GetUserDetailsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<UserDetailsDto>> Handle(GetUserDetailsQuery req, CancellationToken ct)
    {
        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == req.Id, ct);
        if (user is null) return Result<UserDetailsDto>.Fail("کاربر یافت نشد");

        var dto = new UserDetailsDto
        {
            Id = user.Id,
            PhoneNumber = user.PhoneNumber ?? "",
            DisplayUserName = user.DisplayUserName,
            FullName = user.FullName,
            Role = user.Role,
            IsPhoneConfirmed = user.IsPhoneConfirmed,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };

        dto.Orders = await _db.Orders.AsNoTracking()
            .Where(o => o.UserId == user.Id)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new UserOrderDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                Status = o.Status,
                Total = o.Total,
                ItemCount = o.Items.Count,
                CreatedAt = o.CreatedAt
            }).ToListAsync(ct);

        dto.WorkoutRequests = await _db.WorkoutRequests.AsNoTracking()
            .Where(w => w.UserId == user.Id)
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => new UserWorkoutRequestDto
            {
                Id = w.Id,
                QuestionnaireTitle = w.Questionnaire.Title,
                Status = w.Status,
                Amount = w.Amount,
                CreatedAt = w.CreatedAt
            }).ToListAsync(ct);

        dto.Programs = await _db.WorkoutPrograms.AsNoTracking()
            .Where(p => p.UserId == user.Id)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new UserProgramDto
            {
                Id = p.Id,
                RequestId = p.RequestId,
                Title = p.Title,
                DaysCount = p.Days.Count,
                IsVisibleToUser = p.IsVisibleToUser,
                CreatedAt = p.CreatedAt
            }).ToListAsync(ct);

        return Result<UserDetailsDto>.Ok(dto);
    }
}