namespace ProjektPO2.Models;

public class AnswerOption
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int Order { get; set; }

    public int QuestionId { get; set; }
    public ClosedQuestion Question { get; set; } = null!;
}
