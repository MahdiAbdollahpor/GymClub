using GymClub.Domain.Enums;


namespace GymClub.Application.Workouts.Dtos;

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

    // فیلدهای جدید:
    public string? UserPhone { get; set; }
    public string? UserName { get; set; }
    public bool HasProgram { get; set; }
    public string? ProgramTitle { get; set; }
    public int ProgramDaysCount { get; set; }
}