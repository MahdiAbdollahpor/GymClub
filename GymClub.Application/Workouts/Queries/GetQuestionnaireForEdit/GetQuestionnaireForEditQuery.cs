using System.Text.Json;
using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using GymClub.Application.Workouts.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Workouts.Queries.GetQuestionnaireForEdit;

public record GetQuestionnaireForEditQuery(Guid Id) : IRequest<Result<QuestionnaireDto>>;

public class GetQuestionnaireForEditQueryHandler
    : IRequestHandler<GetQuestionnaireForEditQuery, Result<QuestionnaireDto>>
{
    private readonly IApplicationDbContext _db;
    public GetQuestionnaireForEditQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<QuestionnaireDto>> Handle(GetQuestionnaireForEditQuery req, CancellationToken ct)
    {
        var q = await _db.WorkoutQuestionnaires
            .AsNoTracking()
            .Include(x => x.Questions)
            .FirstOrDefaultAsync(x => x.Id == req.Id && !x.IsDeleted, ct);
        if (q is null) return Result<QuestionnaireDto>.Fail("پرسشنامه یافت نشد");

        return Result<QuestionnaireDto>.Ok(new QuestionnaireDto
        {
            Id = q.Id,
            Title = q.Title,
            Description = q.Description,
            Price = q.Price,
            Version = q.Version,
            IsActive = q.IsActive,
            QuestionCount = q.Questions.Count,
            Questions = q.Questions.OrderBy(x => x.SortOrder).Select(x => new QuestionDto
            {
                Id = x.Id,
                QuestionText = x.QuestionText,
                Type = x.Type,
                IsRequired = x.IsRequired,
                SortOrder = x.SortOrder,
                GroupName = x.GroupName,
                Options = string.IsNullOrWhiteSpace(x.OptionsJson)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(x.OptionsJson) ?? new()
            }).ToList()
        });
    }
}