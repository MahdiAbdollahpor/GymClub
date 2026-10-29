using GymClub.Domain.Common;
using GymClub.Domain.Enums;

namespace GymClub.Domain.Entities;

public class WorkoutQuestion : BaseEntity
{
    public Guid QuestionnaireId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public QuestionType Type { get; set; }
    public string? OptionsJson { get; set; }        // برای انتخابی: ["بله","خیر"]
    public bool IsRequired { get; set; }
    public int SortOrder { get; set; }
    public string? GroupName { get; set; }          // "مشخصات بدن" / "سلامت"

    public WorkoutQuestionnaire Questionnaire { get; set; } = null!;
    public ICollection<WorkoutAnswer> Answers { get; set; } = new List<WorkoutAnswer>();
}