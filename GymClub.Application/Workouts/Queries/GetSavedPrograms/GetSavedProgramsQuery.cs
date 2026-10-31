using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Workouts.Queries.GetSavedPrograms;

public class SavedProgramDto
{
    public Guid Id { get; set; }
    public Guid RequestId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserPhone { get; set; } = string.Empty;
    public int DaysCount { get; set; }
    public int ExercisesCount { get; set; }
    public bool IsVisibleToUser { get; set; }
    public DateTime CreatedAt { get; set; }
}

public record GetSavedProgramsQuery(string? Search = null, int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<SavedProgramDto>>;

public class GetSavedProgramsQueryHandler
    : IRequestHandler<GetSavedProgramsQuery, PagedResult<SavedProgramDto>>
{
    private readonly IApplicationDbContext _db;
    public GetSavedProgramsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<SavedProgramDto>> Handle(GetSavedProgramsQuery req, CancellationToken ct)
    {
        var q = _db.WorkoutPrograms.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(req.Search))
            q = q.Where(p => p.Title.Contains(req.Search)
                          || p.Request.User.PhoneNumber!.Contains(req.Search)
                          || p.Request.User.DisplayUserName.Contains(req.Search));

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(p => p.CreatedAt)
            .Skip((req.Page - 1) * req.PageSize).Take(req.PageSize)
            .Select(p => new SavedProgramDto
            {
                Id = p.Id,
                RequestId = p.RequestId,
                Title = p.Title,
                Description = p.Description,
                UserName = p.Request.User.DisplayUserName,
                UserPhone = p.Request.User.PhoneNumber ?? "",
                DaysCount = p.Days.Count,
                ExercisesCount = p.Days.SelectMany(d => d.Exercises).Count(),
                IsVisibleToUser = p.IsVisibleToUser,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync(ct);

        return new PagedResult<SavedProgramDto>
        {
            Items = items,
            Page = req.Page,
            PageSize = req.PageSize,
            TotalCount = total
        };
    }
}