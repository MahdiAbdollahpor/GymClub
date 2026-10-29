using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using GymClub.Application.Workouts.Dtos;
using GymClub.Application.Workouts.Queries.GetMyWorkoutRequest;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Workouts.Queries.GetWorkoutRequestDetails;

public record GetWorkoutRequestDetailsQuery(Guid Id) : IRequest<Result<WorkoutRequestDetailsDto>>;

public class GetWorkoutRequestDetailsQueryHandler
    : IRequestHandler<GetWorkoutRequestDetailsQuery, Result<WorkoutRequestDetailsDto>>
{
    private readonly IApplicationDbContext _db;
    public GetWorkoutRequestDetailsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<WorkoutRequestDetailsDto>> Handle(
        GetWorkoutRequestDetailsQuery req, CancellationToken ct)
    {
        var r = await _db.WorkoutRequests
            .AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.Questionnaire)
            .Include(x => x.Answers).ThenInclude(a => a.Question)
            .Include(x => x.Program).ThenInclude(p => p!.Days).ThenInclude(d => d.Exercises)
            .FirstOrDefaultAsync(x => x.Id == req.Id, ct);

        if (r is null) return Result<WorkoutRequestDetailsDto>.Fail("درخواست یافت نشد");
        return Result<WorkoutRequestDetailsDto>.Ok(GetMyWorkoutRequestQueryHandler.Map(r));
    }
}