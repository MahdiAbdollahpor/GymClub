using FluentValidation;
using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using GymClub.Domain.Entities;
using GymClub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace GymClub.Application.Workouts.Commands.SaveQuestionnaire;

public class QuestionInput
{
    public Guid? Id { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public QuestionType Type { get; set; }
    public List<string> Options { get; set; } = new();
    public bool IsRequired { get; set; }
    public string? GroupName { get; set; }
    public int SortOrder { get; set; }
}

public record SaveQuestionnaireCommand(
    Guid? Id,
    string Title,
    string Description,
    decimal Price,
    bool IsActive,
    List<QuestionInput> Questions
) : IRequest<Result<Guid>>;

public class SaveQuestionnaireValidator : AbstractValidator<SaveQuestionnaireCommand>
{
    public SaveQuestionnaireValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Questions).NotEmpty();
        RuleForEach(x => x.Questions).ChildRules(q =>
        {
            q.RuleFor(x => x.QuestionText).NotEmpty().MaximumLength(500);
            q.When(x => x.Type == QuestionType.SingleChoice || x.Type == QuestionType.MultiChoice,
                () => q.RuleFor(x => x.Options).NotEmpty()
                    .WithMessage("برای سؤال انتخابی، گزینه‌ها الزامی است"));
        });
    }
}

public class SaveQuestionnaireHandler
    : IRequestHandler<SaveQuestionnaireCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    public SaveQuestionnaireHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<Guid>> Handle(SaveQuestionnaireCommand req, CancellationToken ct)
    {
        WorkoutQuestionnaire entity;

        if (req.Id.HasValue)
        {
            var e = await _db.WorkoutQuestionnaires
                .Include(x => x.Questions)
                .FirstOrDefaultAsync(x => x.Id == req.Id && !x.IsDeleted, ct);
            if (e is null) return Result<Guid>.Fail("پرسشنامه یافت نشد");

            // اگر سؤال حذف شده و پاسخ دارد، اجازه حذف نده
            var existingIds = req.Questions.Where(q => q.Id.HasValue).Select(q => q.Id!.Value).ToHashSet();
            foreach (var old in e.Questions.Where(q => !existingIds.Contains(q.Id)))
            {
                var hasAnswers = await _db.WorkoutAnswers.AnyAsync(a => a.QuestionId == old.Id, ct);
                if (hasAnswers)
                    return Result<Guid>.Fail($"سؤال «{old.QuestionText}» پاسخ دارد و قابل حذف نیست");
                _db.WorkoutQuestions.Remove(old);
            }

            entity = e;
            entity.Title = req.Title.Trim();
            entity.Description = req.Description.Trim();
            entity.Price = req.Price;
            entity.IsActive = req.IsActive;
            entity.Version++;
        }
        else
        {
            entity = new WorkoutQuestionnaire
            {
                Title = req.Title.Trim(),
                Description = req.Description.Trim(),
                Price = req.Price,
                IsActive = req.IsActive
            };
            _db.WorkoutQuestionnaires.Add(entity);
        }

        // اگر این پرسشنامه فعال است، بقیه غیرفعال شوند
        if (req.IsActive)
        {
            var others = await _db.WorkoutQuestionnaires
                .Where(x => x.IsActive && x.Id != entity.Id && !x.IsDeleted)
                .ToListAsync(ct);
            foreach (var o in others) o.IsActive = false;
        }

        await _db.SaveChangesAsync(ct);

        foreach (var q in req.Questions.OrderBy(x => x.SortOrder))
        {
            WorkoutQuestion qe;
            if (q.Id.HasValue)
            {
                qe = await _db.WorkoutQuestions.FirstOrDefaultAsync(x => x.Id == q.Id, ct)
                     ?? new WorkoutQuestion { QuestionnaireId = entity.Id };
            }
            else
            {
                qe = new WorkoutQuestion { QuestionnaireId = entity.Id };
                _db.WorkoutQuestions.Add(qe);
            }

            qe.QuestionText = q.QuestionText.Trim();
            qe.Type = q.Type;
            qe.IsRequired = q.IsRequired;
            qe.GroupName = q.GroupName?.Trim();
            qe.SortOrder = q.SortOrder;
            qe.OptionsJson = (q.Type == QuestionType.SingleChoice || q.Type == QuestionType.MultiChoice)
                ? JsonSerializer.Serialize(q.Options.Where(o => !string.IsNullOrWhiteSpace(o)).ToList())
                : null;
        }

        await _db.SaveChangesAsync(ct);
        return Result<Guid>.Ok(entity.Id);
    }
}