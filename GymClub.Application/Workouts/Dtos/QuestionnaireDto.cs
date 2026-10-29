using GymClub.Domain.Enums;

namespace GymClub.Application.Workouts.Dtos;

public class QuestionnaireDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Version { get; set; }
    public int QuestionCount { get; set; }
    public bool IsActive { get; set; }
    public List<QuestionDto> Questions { get; set; } = new();
}

public class QuestionDto
{
    public Guid Id { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public QuestionType Type { get; set; }
    public List<string> Options { get; set; } = new();
    public bool IsRequired { get; set; }
    public int SortOrder { get; set; }
    public string? GroupName { get; set; }
}

public class WorkoutRequestListItemDto
{
    public Guid Id { get; set; }
    public Guid QuestionnaireId { get; set; }
    public string QuestionnaireTitle { get; set; } = string.Empty;
    public WorkoutRequestStatus Status { get; set; }
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class WorkoutRequestDetailsDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserPhone { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public Guid QuestionnaireId { get; set; }
    public string QuestionnaireTitle { get; set; } = string.Empty;
    public WorkoutRequestStatus Status { get; set; }
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? AdminNote { get; set; }
    public List<WorkoutAnswerDto> Answers { get; set; } = new();
    public WorkoutProgramDto? Program { get; set; }
}

public class WorkoutAnswerDto
{
    public Guid QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public QuestionType Type { get; set; }
    public string? GroupName { get; set; }
    public string DisplayValue { get; set; } = string.Empty;
    public string? AnswerText { get; set; }
    public decimal? AnswerNumber { get; set; }
    public bool? AnswerBool { get; set; }
    public List<string> AnswerList { get; set; } = new();
}

public class WorkoutProgramDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsVisibleToUser { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<WorkoutDayDto> Days { get; set; } = new();
}

public class WorkoutDayDto
{
    public Guid Id { get; set; }
    public int DayNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public List<WorkoutExerciseDto> Exercises { get; set; } = new();
}

public class WorkoutExerciseDto
{
    public Guid Id { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public int? Sets { get; set; }
    public string? Reps { get; set; }
    public int? RestSeconds { get; set; }
    public string? Weight { get; set; }
    public string? Notes { get; set; }
    public string? VideoUrl { get; set; }
    public int SortOrder { get; set; }
}