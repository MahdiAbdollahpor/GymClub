using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using GymClub.Application.Workouts.Dtos;
using GymClub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Workouts.Queries.GetAllWorkoutRequests;

public record GetAllWorkoutRequestsQuery(
    string? Search = null,
    WorkoutRequestStatus? Status = null,
    int Page = 1, int PageSize = 20
) : IRequest<PagedResult<WorkoutRequestListItemDto>>;

public class GetAllWorkoutRequestsQueryHandler
    : IRequestHandler<GetAllWorkoutRequestsQuery, PagedResult<WorkoutRequestListItemDto>>
{
    private readonly IApplicationDbContext _db;
    public GetAllWorkoutRequestsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<WorkoutRequestListItemDto>> Handle(
        GetAllWorkoutRequestsQuery req, CancellationToken ct)
    {
        var q = _db.WorkoutRequests.AsNoTracking().AsQueryable();

        if (req.Status.HasValue)
            q = q.Where(x => x.Status == req.Status);

        if (!string.IsNullOrWhiteSpace(req.Search))
            q = q.Where(x => x.User.PhoneNumber!.Contains(req.Search)
                          || x.User.DisplayUserName.Contains(req.Search));

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(x => x.CreatedAt)
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .Select(x => new WorkoutRequestListItemDto
            {
                Id = x.Id,
                QuestionnaireId = x.QuestionnaireId,
                QuestionnaireTitle = x.Questionnaire.Title,
                Status = x.Status,
                Amount = x.Amount,
                CreatedAt = x.CreatedAt,
                PaidAt = x.PaidAt,
                CompletedAt = x.CompletedAt
            })
            .ToListAsync(ct);

        return new PagedResult<WorkoutRequestListItemDto>
        {
            Items = items,
            Page = req.Page,
            PageSize = req.PageSize,
            TotalCount = total
        };
    }
}