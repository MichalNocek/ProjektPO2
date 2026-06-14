namespace ProjektPO2.Models;

public class StudentGroup
{
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public int GroupId { get; set; }
    public Group Group { get; set; } = null!;
}
