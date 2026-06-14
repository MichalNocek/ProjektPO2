namespace ProjektPO2.Models;

public class QuizAttempt
{
    public int Id { get; set; }
    public int QuizId { get; set; }
    public int StudentId { get; set; }
    public int Score { get; set; }
    public int MaxScore { get; set; }
    public int Pct { get; set; }
    public bool Passed { get; set; }
    public bool Pending { get; set; }
    public bool Cheated { get; set; }
    public int TimeUsed { get; set; }
    public DateTime DateTaken { get; set; }

    public Quiz Quiz { get; set; } = null!;
    public Student Student { get; set; } = null!;
    public ICollection<StudentAnswer> Answers { get; set; } = new List<StudentAnswer>();
}
