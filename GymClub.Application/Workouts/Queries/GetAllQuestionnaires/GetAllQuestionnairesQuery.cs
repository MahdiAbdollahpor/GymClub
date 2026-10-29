using GymClub.Application.Common.Interfaces;
using GymClub.Application.Workouts.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Workouts.Queries.GetAllQuestionnaires;

public record GetAllQuestionnairesQuery : IRequest<List<QuestionnaireDto>>;

public class GetAllQuestionnairesQueryHandler
    : IRequestHandler<GetAllQuestionnairesQuery, List<QuestionnaireDto>>
{
    private readonly IApplicationDbContext _db;
    public GetAllQuestionnairesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<QuestionnaireDto>> Handle(GetAllQuestionnairesQuery req, CancellationToken ct)
    {
        return await _db.WorkoutQuestionnaires
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.Version)
            .Select(x => new QuestionnaireDto
            {
                Id = x.Id,
                Title = x.Title,
                Description = x.Description,
                Price = x.Price,
                Version = x.Version,
                IsActive = x.IsActive,
                QuestionCount = x.Questions.Count
            })
            .ToListAsync(ct);
    }
}