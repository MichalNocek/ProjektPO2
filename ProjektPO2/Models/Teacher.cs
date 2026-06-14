namespace ProjektPO2.Models;

public class Teacher : User
{
    public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();

    public override string GetRoleLabel() => "Nauczyciel";
}
