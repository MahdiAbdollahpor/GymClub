using System.Text.Json;
using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using GymClub.Application.Workouts.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Workouts.Queries.GetActiveQuestionnaire;

public record GetActiveQuestionnaireQuery : IRequest<Result<QuestionnaireDto>>;

public class GetActiveQuestionnaireQueryHandler
    : IRequestHandler<GetActiveQuestionnaireQuery, Result<QuestionnaireDto>>
{
    private readonly IApplicationDbContext _db;
    public GetActiveQuestionnaireQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<QuestionnaireDto>> Handle(GetActiveQuestionnaireQuery req, CancellationToken ct)
    {
        var q = await _db.WorkoutQuestionnaires
            .AsNoTracking()
            .Include(x => x.Questions)
            .Where(x => !x.IsDeleted && x.IsActive)
            .OrderByDescending(x => x.Version)
            .FirstOrDefaultAsync(ct);

        if (q is null) return Result<QuestionnaireDto>.Fail("پرسشنامه فعالی وجود ندارد");

        return Result<QuestionnaireDto>.Ok(new QuestionnaireDto
        {
            Id = q.Id,
            Title = q.Title,
            Description = q.Description,
            Price = q.Price,
            Version = q.Version,
            QuestionCount = q.Questions.Count,
            IsActive = q.IsActive,
            Questions = q.Questions
                .OrderBy(x => x.SortOrder)
                .Select(x => new QuestionDto
                {
                    Id = x.Id,
                    QuestionText = x.QuestionText,
                    Type = x.Type,
                    IsRequired = x.IsRequired,
                    SortOrder = x.SortOrder,
                    GroupName = x.GroupName,
                    Options = string.IsNullOrWhiteSpace(x.OptionsJson)
                        ? new List<string>()
                        : (JsonSerializer.Deserialize<List<string>>(x.OptionsJson) ?? new List<string>())
                }).ToList()
        });
    }
}