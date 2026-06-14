namespace ProjektPO2.Models;

public class TestGroup
{
    public int QuizId { get; set; }
    public Quiz Quiz { get; set; } = null!;

    public int GroupId { get; set; }
    public Group Group { get; set; } = null!;
}
