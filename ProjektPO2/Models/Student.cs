namespace ProjektPO2.Models;

public class Student : User
{
    public ICollection<StudentGroup> StudentGroups { get; set; } = new List<StudentGroup>();
    public ICollection<QuizAttempt> Attempts { get; set; } = new List<QuizAttempt>();

    public override string GetRoleLabel() => "Uczeń";
}
