using GymClub.Domain.Common;

namespace GymClub.Domain.Entities;

public class WorkoutProgram : BaseEntity
{
    public Guid RequestId { get; set; }
    public Guid UserId { get; set; }
    public Guid AdminId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsVisibleToUser { get; set; } = true;

    public WorkoutRequest Request { get; set; } = null!;
    public ICollection<WorkoutProgramDay> Days { get; set; } = new List<WorkoutProgramDay>();
}