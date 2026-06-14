namespace ProjektPO2.Models;

public class OpenQuestion : Question
{
    public string ModelAnswer { get; set; } = "";

    public override string GetTypeLabel() => "Otwarte";
    public override bool RequiresManualGrading => true;
}
