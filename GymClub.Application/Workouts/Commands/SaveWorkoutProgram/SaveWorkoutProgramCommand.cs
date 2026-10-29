using FluentValidation;
using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using GymClub.Domain.Entities;
using GymClub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Workouts.Commands.SaveWorkoutProgram;

public class ProgramExerciseInput
{
    public string ExerciseName { get; set; } = string.Empty;
    public int? Sets { get; set; }
    public string? Reps { get; set; }
    public int? RestSeconds { get; set; }
    public string? Weight { get; set; }
    public string? Notes { get; set; }
    public string? VideoUrl { get; set; }
}

public class ProgramDayInput
{
    public int DayNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public List<ProgramExerciseInput> Exercises { get; set; } = new();
}

public record SaveWorkoutProgramCommand(
    Guid RequestId,
    string Title,
    string? Description,
    DateTime? StartDate,
    DateTime? EndDate,
    bool Publish,
    List<ProgramDayInput> Days
) : IRequest<Result<Guid>>;

public class SaveWorkoutProgramValidator : AbstractValidator<SaveWorkoutProgramCommand>
{
    public SaveWorkoutProgramValidator()
    {
        RuleFor(x => x.RequestId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Days).NotEmpty().WithMessage("حداقل یک روز تمرینی وارد کنید");
        RuleForEach(x => x.Days).ChildRules(d =>
        {
            d.RuleFor(x => x.DayNumber).GreaterThan(0);
            d.RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        });
    }
}

public class SaveWorkoutProgramCommandHandler
    : IRequestHandler<SaveWorkoutProgramCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _current;
    private readonly IDateTimeProvider _clock;

    public SaveWorkoutProgramCommandHandler(
        IApplicationDbContext db, ICurrentUserService current, IDateTimeProvider clock)
    {
        _db = db; _current = current; _clock = clock;
    }

    public async Task<Result<Guid>> Handle(SaveWorkoutProgramCommand req, CancellationToken ct)
    {
        if (_current.UserId is null) return Result<Guid>.Fail("ابتدا وارد شوید");

        var request = await _db.WorkoutRequests
            .Include(x => x.Program)
            .FirstOrDefaultAsync(x => x.Id == req.RequestId, ct);
        if (request is null) return Result<Guid>.Fail("درخواست یافت نشد");

        var adminId = _current.UserId.Value;
        var now = _clock.UtcNow;

        WorkoutProgram program;
        if (request.Program is null)
        {
            program = new WorkoutProgram
            {
                RequestId = request.Id,
                UserId = request.UserId,
                AdminId = adminId,
                Title = req.Title.Trim(),
                Description = req.Description?.Trim(),
                StartDate = req.StartDate,
                EndDate = req.EndDate,
                IsVisibleToUser = req.Publish
            };
            _db.WorkoutPrograms.Add(program);
        }
        else
        {
            program = request.Program;
            program.Title = req.Title.Trim();
            program.Description = req.Description?.Trim();
            program.StartDate = req.StartDate;
            program.EndDate = req.EndDate;
            program.IsVisibleToUser = req.Publish;

            // حذف روزهای قبلی
            _db.WorkoutProgramDays.RemoveRange(
                await _db.WorkoutProgramDays.Where(d => d.ProgramId == program.Id).ToListAsync(ct));
        }

        await _db.SaveChangesAsync(ct);

        foreach (var d in req.Days.OrderBy(x => x.DayNumber))
        {
            var day = new WorkoutProgramDay
            {
                ProgramId = program.Id,
                DayNumber = d.DayNumber,
                Title = d.Title.Trim(),
                Notes = d.Notes
            };
            _db.WorkoutProgramDays.Add(day);
            await _db.SaveChangesAsync(ct);

            var order = 0;
            foreach (var e in d.Exercises)
            {
                _db.WorkoutExercises.Add(new WorkoutExercise
                {
                    ProgramDayId = day.Id,
                    ExerciseName = e.ExerciseName.Trim(),
                    Sets = e.Sets,
                    Reps = e.Reps,
                    RestSeconds = e.RestSeconds,
                    Weight = e.Weight,
                    Notes = e.Notes,
                    VideoUrl = e.VideoUrl,
                    SortOrder = order++
                });
            }
        }

        if (req.Publish)
        {
            request.Status = WorkoutRequestStatus.Completed;
            request.CompletedAt = now;
        }

        await _db.SaveChangesAsync(ct);
        return Result<Guid>.Ok(program.Id);
    }
}