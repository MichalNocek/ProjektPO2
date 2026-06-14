namespace ProjektPO2.Models;

public class ClosedQuestion : Question
{
    public bool IsMultipleChoice { get; set; }
    public ICollection<AnswerOption> Options { get; set; } = new List<AnswerOption>();

    public override string GetTypeLabel() =>
        IsMultipleChoice ? "Zamknięte (wielokrotny wybór)" : "Zamknięte (jednokrotny wybór)";

    public override bool RequiresManualGrading => false;
}
