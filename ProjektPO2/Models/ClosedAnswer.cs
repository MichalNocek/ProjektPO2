namespace ProjektPO2.Models;

public class ClosedAnswer : StudentAnswer
{
    public ICollection<SelectedOption> SelectedOptions { get; set; } = new List<SelectedOption>();
}
