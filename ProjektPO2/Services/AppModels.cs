using System.Collections.Generic;
using System.Linq;

namespace ProjektPO2.Services;

public enum QuestionType { Single, Multi, Open }

// DTO — used by Views; mapped from EF Core entities in DbRepository
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Login { get; set; } = "";
    public string Password { get; set; } = "";
    public string Role { get; set; } = "student";
    public List<string> GroupNames { get; set; } = new();

    public string GroupName => GroupNames.Count > 0 ? string.Join(", ", GroupNames) : "";

    public bool IsTeacher => Role == "teacher";

    public string Initials
    {
        get
        {
            var parts = Name.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
            string s = parts.Length > 0 ? parts[0].Substring(0, 1) : "";
            if (parts.Length > 1) s += parts[1].Substring(0, 1);
            return s.ToUpper();
        }
    }
}

public class Group
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public List<int> StudentIds { get; set; } = new();
}

public class Question
{
    public int Id { get; set; }
    public int TestId { get; set; }
    public QuestionType Type { get; set; } = QuestionType.Single;
    public string Text { get; set; } = "";
    public int Points { get; set; } = 1;
    public List<string> Options { get; set; } = new();
    public List<int> Correct { get; set; } = new();
    public string Sample { get; set; } = "";
    public int OrderIdx { get; set; }
}

public class Test
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Subject { get; set; } = "";
    public int TimeLimit { get; set; } = 20;
    public int PassThreshold { get; set; } = 50;
    public int MaxAttempts { get; set; } = 1;
    public bool IsActive { get; set; } = false; // widoczny dla uczniów (mapuje na Quiz.IsPublished)
    public string ColorHex { get; set; } = "#2563EB";
    public List<int> GroupIds { get; set; } = new();
    public List<Question> Questions { get; set; } = new();

    public int MaxPoints
    {
        get { int s = 0; foreach (var q in Questions) s += q.Points; return s; }
    }
}

public class Result
{
    public int Id { get; set; }
    public int TestId { get; set; }
    public int StudentId { get; set; }
    public int Score { get; set; }
    public int MaxScore { get; set; }
    public int Pct { get; set; }
    public bool Passed { get; set; }
    public int TimeUsed { get; set; }
    public System.DateTime DateTaken { get; set; }
    public Dictionary<int, object> Answers { get; set; } = new();
    public Dictionary<int, int> Manual { get; set; } = new();
    public bool Pending { get; set; }
    public bool Cheated { get; set; }
}
