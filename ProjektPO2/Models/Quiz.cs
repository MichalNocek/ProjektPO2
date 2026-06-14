using System.ComponentModel.DataAnnotations.Schema;

namespace ProjektPO2.Models;

public class Quiz : IPublishable
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Subject { get; set; } = "Matematyka";
    public int TimeLimit { get; set; } = 20;
    public int PassThreshold { get; set; } = 50;
    public int MaxAttempts { get; set; } = 1;
    public string ColorHex { get; set; } = "#2563EB";
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int TeacherId { get; set; }
    public Teacher Teacher { get; set; } = null!;

    public ICollection<Question> Questions { get; set; } = new List<Question>();
    public ICollection<QuizAttempt> Attempts { get; set; } = new List<QuizAttempt>();
    public ICollection<TestGroup> TestGroups { get; set; } = new List<TestGroup>();

    [NotMapped]
    public int MaxPoints => Questions.Sum(q => q.Points);

    public void Publish() => IsPublished = true;
    public void Unpublish() => IsPublished = false;
}
