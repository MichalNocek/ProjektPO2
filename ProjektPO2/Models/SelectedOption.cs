namespace ProjektPO2.Models;

public class SelectedOption
{
    public int Id { get; set; }

    public int ClosedAnswerId { get; set; }
    public ClosedAnswer ClosedAnswer { get; set; } = null!;

    public int OptionId { get; set; }
    public AnswerOption Option { get; set; } = null!;
}
