using GymClub.Domain.Common;

namespace GymClub.Domain.Entities;

public class WorkoutExercise : BaseEntity
{
    public Guid ProgramDayId { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public int? Sets { get; set; }
    public string? Reps { get; set; }
    public int? RestSeconds { get; set; }
    public string? Weight { get; set; }
    public string? Notes { get; set; }
    public string? VideoUrl { get; set; }
    public int SortOrder { get; set; }

    public WorkoutProgramDay ProgramDay { get; set; } = null!;
}