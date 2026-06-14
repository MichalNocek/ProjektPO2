namespace ProjektPO2.Models;

public abstract class StudentAnswer
{
    public int Id { get; set; }
    public int? PointsAwarded { get; set; }

    public int AttemptId { get; set; }
    public QuizAttempt Attempt { get; set; } = null!;

    public int QuestionId { get; set; }
    public Question Question { get; set; } = null!;
}
