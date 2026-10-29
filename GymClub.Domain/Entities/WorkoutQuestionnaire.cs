using GymClub.Domain.Common;

namespace GymClub.Domain.Entities;

public class WorkoutQuestionnaire : SoftDeletableEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
    public int Version { get; set; } = 1;

    public ICollection<WorkoutQuestion> Questions { get; set; } = new List<WorkoutQuestion>();
    public ICollection<WorkoutRequest> Requests { get; set; } = new List<WorkoutRequest>();
}