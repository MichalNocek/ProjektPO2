namespace ProjektPO2.Models;

public abstract class Question
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public int Points { get; set; } = 1;
    public int Order { get; set; }

    public int QuizId { get; set; }
    public Quiz Quiz { get; set; } = null!;

    public abstract string GetTypeLabel();
    public abstract bool RequiresManualGrading { get; }
}
