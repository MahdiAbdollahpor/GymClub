using GymClub.Domain.Common;

namespace GymClub.Domain.Entities;

public class WorkoutProgramDay : BaseEntity
{
    public Guid ProgramId { get; set; }
    public int DayNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Notes { get; set; }

    public WorkoutProgram Program { get; set; } = null!;
    public ICollection<WorkoutExercise> Exercises { get; set; } = new List<WorkoutExercise>();
}