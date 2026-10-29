namespace GymClub.Domain.Enums;

public enum WorkoutRequestStatus
{
    PendingPayment = 0,
    Paid = 1,
    AnswersSubmitted = 2,
    Completed = 3,
    Cancelled = 4
}