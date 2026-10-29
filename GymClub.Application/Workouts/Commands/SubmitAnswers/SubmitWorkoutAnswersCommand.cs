using System.Text.Json;
using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using GymClub.Domain.Entities;
using GymClub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Workouts.Commands.SubmitAnswers;

public record AnswerInput(Guid QuestionId, string? Text, decimal? Number, bool? Bool, List<string>? List);

public record SubmitWorkoutAnswersCommand(List<AnswerInput> Answers)
    : IRequest<Result<Guid>>;

public class SubmitWorkoutAnswersCommandHandler
    : IRequestHandler<SubmitWorkoutAnswersCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _current;
    private readonly IDateTimeProvider _clock;

    public SubmitWorkoutAnswersCommandHandler(
        IApplicationDbContext db, ICurrentUserService current, IDateTimeProvider clock)
    {
        _db = db; _current = current; _clock = clock;
    }

    public async Task<Result<Guid>> Handle(SubmitWorkoutAnswersCommand req, CancellationToken ct)
    {
        if (_current.UserId is null) return Result<Guid>.Fail("ابتدا وارد شوید");

        var request = await _db.WorkoutRequests
            .Include(x => x.Questionnaire).ThenInclude(q => q.Questions)
            .Include(x => x.Answers)
            .Where(x => x.UserId == _current.UserId.Value
                     && x.Status == WorkoutRequestStatus.Paid)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);
        if (request is null) return Result<Guid>.Fail("درخواست معتبری وجود ندارد");

        // حذف پاسخ‌های قبلی (اگر کاربر دوباره ارسال کرد)
        _db.WorkoutAnswers.RemoveRange(request.Answers);

        foreach (var q in request.Questionnaire.Questions)
        {
            var ans = req.Answers.FirstOrDefault(a => a.QuestionId == q.Id);
            if (ans is null)
            {
                if (q.IsRequired) return Result<Guid>.Fail($"پاسخ سؤال «{q.QuestionText}» الزامی است");
                continue;
            }

            var entity = new WorkoutAnswer
            {
                RequestId = request.Id,
                QuestionId = q.Id,
                AnswerText = ans.Text?.Trim(),
                AnswerNumber = ans.Number,
                AnswerBool = ans.Bool,
                AnswerJson = ans.List is { Count: > 0 }
                    ? JsonSerializer.Serialize(ans.List)
                    : null
            };
            _db.WorkoutAnswers.Add(entity);
        }

        request.Status = WorkoutRequestStatus.AnswersSubmitted;
        request.SubmittedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Result<Guid>.Ok(request.Id);
    }
}