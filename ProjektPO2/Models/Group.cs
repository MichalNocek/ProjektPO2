namespace ProjektPO2.Models;

public class Group
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";

    public ICollection<TestGroup> TestGroups { get; set; } = new List<TestGroup>();
    public ICollection<StudentGroup> StudentGroups { get; set; } = new List<StudentGroup>();
}
