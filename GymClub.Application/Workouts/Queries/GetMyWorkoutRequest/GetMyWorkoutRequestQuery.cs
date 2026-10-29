using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using GymClub.Application.Workouts.Dtos;
using GymClub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Workouts.Queries.GetMyWorkoutRequest;

public record GetMyWorkoutRequestQuery : IRequest<Result<WorkoutRequestDetailsDto>>;

public class GetMyWorkoutRequestQueryHandler
    : IRequestHandler<GetMyWorkoutRequestQuery, Result<WorkoutRequestDetailsDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _current;

    public GetMyWorkoutRequestQueryHandler(IApplicationDbContext db, ICurrentUserService current)
    {
        _db = db; _current = current;
    }

    public async Task<Result<WorkoutRequestDetailsDto>> Handle(GetMyWorkoutRequestQuery req, CancellationToken ct)
    {
        if (_current.UserId is null) return Result<WorkoutRequestDetailsDto>.Fail("ابتدا وارد شوید");

        var r = await _db.WorkoutRequests
            .AsNoTracking()
            .Include(x => x.Questionnaire)
            .Include(x => x.Answers).ThenInclude(a => a.Question)
            .Include(x => x.Program).ThenInclude(p => p!.Days).ThenInclude(d => d.Exercises)
            .Where(x => x.UserId == _current.UserId.Value
                     && x.Status != WorkoutRequestStatus.Cancelled)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (r is null) return Result<WorkoutRequestDetailsDto>.Fail("درخواستی وجود ندارد");

        return Result<WorkoutRequestDetailsDto>.Ok(Map(r));
    }

    internal static WorkoutRequestDetailsDto Map(Domain.Entities.WorkoutRequest r)
    {
        var dto = new WorkoutRequestDetailsDto
        {
            Id = r.Id,
            UserId = r.UserId,
            UserPhone = r.User?.PhoneNumber ?? "",
            UserName = r.User?.DisplayUserName ?? "",
            QuestionnaireId = r.QuestionnaireId,
            QuestionnaireTitle = r.Questionnaire?.Title ?? "",
            Status = r.Status,
            Amount = r.Amount,
            CreatedAt = r.CreatedAt,
            PaidAt = r.PaidAt,
            SubmittedAt = r.SubmittedAt,
            CompletedAt = r.CompletedAt,
            AdminNote = r.AdminNote,
            Answers = r.Answers.OrderBy(a => a.Question.SortOrder).Select(a => new WorkoutAnswerDto
            {
                QuestionId = a.QuestionId,
                QuestionText = a.Question.QuestionText,
                Type = a.Question.Type,
                GroupName = a.Question.GroupName,
                AnswerText = a.AnswerText,
                AnswerNumber = a.AnswerNumber,
                AnswerBool = a.AnswerBool,
                AnswerList = string.IsNullOrWhiteSpace(a.AnswerJson)
                    ? new List<string>()
                    : System.Text.Json.JsonSerializer.Deserialize<List<string>>(a.AnswerJson) ?? new List<string>(),
                DisplayValue = BuildDisplayValue(a)
            }).ToList()
        };

        if (r.Program is not null)
        {
            dto.Program = new WorkoutProgramDto
            {
                Id = r.Program.Id,
                Title = r.Program.Title,
                Description = r.Program.Description,
                StartDate = r.Program.StartDate,
                EndDate = r.Program.EndDate,
                IsVisibleToUser = r.Program.IsVisibleToUser,
                CreatedAt = r.Program.CreatedAt,
                Days = r.Program.Days.OrderBy(d => d.DayNumber).Select(d => new WorkoutDayDto
                {
                    Id = d.Id,
                    DayNumber = d.DayNumber,
                    Title = d.Title,
                    Notes = d.Notes,
                    Exercises = d.Exercises.OrderBy(e => e.SortOrder).Select(e => new WorkoutExerciseDto
                    {
                        Id = e.Id,
                        ExerciseName = e.ExerciseName,
                        Sets = e.Sets,
                        Reps = e.Reps,
                        RestSeconds = e.RestSeconds,
                        Weight = e.Weight,
                        Notes = e.Notes,
                        VideoUrl = e.VideoUrl,
                        SortOrder = e.SortOrder
                    }).ToList()
                }).ToList()
            };
        }

        return dto;
    }

    private static string BuildDisplayValue(Domain.Entities.WorkoutAnswer a) => a.Question.Type switch
    {
        QuestionType.Text => a.AnswerText ?? "—",
        QuestionType.Number => a.AnswerNumber?.ToString("0.##") ?? "—",
        QuestionType.Boolean => a.AnswerBool == true ? "بله" : "خیر",
        QuestionType.SingleChoice => a.AnswerText ?? "—",
        QuestionType.MultiChoice => string.IsNullOrWhiteSpace(a.AnswerJson)
            ? "—"
            : string.Join("، ", System.Text.Json.JsonSerializer.Deserialize<List<string>>(a.AnswerJson) ?? new()),
        _ => "—"
    };
}