using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using ProjektPO2.Data;
using Entities = ProjektPO2.Models;

namespace ProjektPO2.Services;

public static class DbRepository
{
    private static AppDbContext Ctx() => new AppDbContext();

    // ---------- INIT ----------
    public static void Init()
    {
        using var ctx = Ctx();
        // Tworzy bazę i nakłada wszystkie migracje EF Core
        // (tabela __EFMigrationsHistory śledzi zastosowane migracje).
        ctx.Database.Migrate();
        DbSeeder.Seed(ctx);
    }

    // ---------- USERS ----------
    public static User? LoginByCredentials(string username, string password)
    {
        using var ctx = Ctx();
        var hash = PasswordHash.Hash(password);
        var u = ctx.Users.FirstOrDefault(x => x.Username == username && x.Password == hash);
        if (u == null) return null;
        if (u is Entities.Student student)
            ctx.Entry(student).Collection(s => s.StudentGroups).Query().Include(sg => sg.Group).Load();
        return ToDto(u);
    }

    public static bool UsernameExists(string username)
    {
        using var ctx = Ctx();
        return ctx.Users.Any(x => x.Username == username);
    }

    public static bool GroupExists(string name)
    {
        using var ctx = Ctx();
        return ctx.Groups.Any(g => g.Name == name);
    }

    public static void CreateGroup(string name, string description)
    {
        using var ctx = Ctx();
        ctx.Groups.Add(new Entities.Group { Name = name, Description = description });
        ctx.SaveChanges();
    }

    public static void AddStudentToGroup(int studentId, int groupId)
    {
        using var ctx = Ctx();
        if (!ctx.StudentGroups.Any(sg => sg.StudentId == studentId && sg.GroupId == groupId))
        {
            ctx.StudentGroups.Add(new Entities.StudentGroup { StudentId = studentId, GroupId = groupId });
            ctx.SaveChanges();
        }
    }

    public static void RemoveStudentFromGroup(int studentId, int groupId)
    {
        using var ctx = Ctx();
        var sg = ctx.StudentGroups.FirstOrDefault(sg => sg.StudentId == studentId && sg.GroupId == groupId);
        if (sg != null)
        {
            ctx.StudentGroups.Remove(sg);
            ctx.SaveChanges();
        }
    }

    public static User RegisterStudent(string firstName, string lastName, string username, string password)
    {
        using var ctx = Ctx();
        var student = new Entities.Student
        {
            FirstName = firstName,
            LastName  = lastName,
            Username  = username,
            Password  = PasswordHash.Hash(password)
        };
        ctx.Users.Add(student);
        ctx.SaveChanges();
        return ToDto(student);
    }

    public static User RegisterTeacher(string firstName, string lastName, string username, string password)
    {
        using var ctx = Ctx();
        var teacher = new Entities.Teacher
        {
            FirstName = firstName,
            LastName  = lastName,
            Username  = username,
            Password  = PasswordHash.Hash(password)
        };
        ctx.Users.Add(teacher);
        ctx.SaveChanges();
        return ToDto(teacher);
    }

    public static List<User> GetStudents()
    {
        using var ctx = Ctx();
        return ctx.Users.OfType<Entities.Student>()
            .Include(s => s.StudentGroups)
            .ThenInclude(sg => sg.Group)
            .OrderBy(s => s.FirstName)
            .ToList()
            .Select(s => ToDto(s))
            .ToList();
    }

    public static User GetTeacher()
    {
        using var ctx = Ctx();
        var t = ctx.Users.OfType<Entities.Teacher>().First();
        return ToDto(t);
    }

    // ---------- GROUPS ----------
    public static List<Group> GetGroups()
    {
        using var ctx = Ctx();
        return ctx.Groups
            .Include(g => g.StudentGroups)
            .ToList()
            .Select(g => new Group
            {
                Id          = g.Id,
                Name        = g.Name,
                Description = g.Description,
                StudentIds  = g.StudentGroups.Select(sg => sg.StudentId).ToList()
            })
            .OrderBy(g => g.Name)
            .ToList();
    }

    // ---------- TESTS ----------
    public static List<Test> GetAllTests()
    {
        using var ctx = Ctx();
        var quizzes = ctx.Quizzes
            .Include(q => q.Questions)
            .ThenInclude(q => ((Entities.ClosedQuestion)q).Options)
            .Include(q => q.TestGroups)
            .OrderBy(q => q.Id)
            .ToList();
        return quizzes.Select(ToTestDto).ToList();
    }

    // Testy należące do konkretnego nauczyciela (widoki panelu nauczyciela).
    public static List<Test> GetTestsForTeacher(int teacherId)
    {
        using var ctx = Ctx();
        var quizzes = ctx.Quizzes
            .Where(q => q.TeacherId == teacherId)
            .Include(q => q.Questions)
            .ThenInclude(q => ((Entities.ClosedQuestion)q).Options)
            .Include(q => q.TestGroups)
            .OrderBy(q => q.Id)
            .ToList();
        return quizzes.Select(ToTestDto).ToList();
    }

    public static Test? GetTest(int id)
    {
        using var ctx = Ctx();
        var q = ctx.Quizzes
            .Include(q => q.Questions)
            .ThenInclude(q => ((Entities.ClosedQuestion)q).Options)
            .Include(q => q.TestGroups)
            .FirstOrDefault(q => q.Id == id);
        return q == null ? null : ToTestDto(q);
    }

    public static List<Test> GetTestsForStudent(User student)
    {
        if (student.GroupNames.Count == 0) return new List<Test>();
        var allGroups = GetGroups();
        var studentGroupIds = allGroups
            .Where(g => student.GroupNames.Contains(g.Name))
            .Select(g => g.Id)
            .ToHashSet();
        var groupTests = GetAllTests()
            .Where(t => t.GroupIds.Any(gid => studentGroupIds.Contains(gid)))
            .ToList();

        using var ctx = Ctx();
        var attemptedIds = ctx.QuizAttempts
            .Where(a => a.StudentId == student.Id)
            .Select(a => a.QuizId)
            .Distinct()
            .ToHashSet();

        // Aktywne testy widać zawsze; nieaktywne tylko gdy uczeń już je rozwiązał
        // (żeby zachował dostęp do swojego wyniku w historii).
        return groupTests.Where(t => t.IsActive || attemptedIds.Contains(t.Id)).ToList();
    }

    // Zmienia widoczność testu dla uczniów (wykorzystuje metody interfejsu IPublishable).
    public static void SetTestActive(int testId, bool active)
    {
        using var ctx = Ctx();
        var quiz = ctx.Quizzes.Find(testId);
        if (quiz == null) return;
        if (active) quiz.Publish(); else quiz.Unpublish();
        ctx.SaveChanges();
    }

    // ---------- RESULTS ----------
    public static List<Result> GetResultsForTest(int testId)
    {
        using var ctx = Ctx();
        var attempts = LoadAttemptsQuery(ctx)
            .Where(a => a.QuizId == testId)
            .OrderByDescending(a => a.DateTaken)
            .ToList();
        LoadSelectedOptions(ctx, attempts);
        return attempts.Select(ToResultDto).ToList();
    }

    public static List<Result> GetAllResults()
    {
        using var ctx = Ctx();
        var attempts = LoadAttemptsQuery(ctx)
            .OrderByDescending(a => a.DateTaken)
            .ToList();
        LoadSelectedOptions(ctx, attempts);
        return attempts.Select(ToResultDto).ToList();
    }

    public static List<(Result result, Test test, User student, List<Question> openQs)> PendingSubmissions(int teacherId)
    {
        var students = GetStudents().ToDictionary(s => s.Id);
        var tests    = GetTestsForTeacher(teacherId).ToDictionary(t => t.Id);
        var output   = new List<(Result, Test, User, List<Question>)>();
        foreach (var r in GetAllResults())
        {
            if (r.Cheated) continue; // próby oszustwa nie trafiają do kolejki oceniania
            if (!tests.TryGetValue(r.TestId, out var t)) continue;
            var pend = GradingService.PendingOpen(t, r);
            if (pend.Count > 0 && students.TryGetValue(r.StudentId, out var st))
                output.Add((r, t, st, pend));
        }
        return output;
    }

    // Liczba podejść danego ucznia do testu (wlicza próby oszustwa).
    public static int CountAttempts(int testId, int studentId)
    {
        using var ctx = Ctx();
        return ctx.QuizAttempts.Count(a => a.QuizId == testId && a.StudentId == studentId);
    }

    // Najlepsze podejście ucznia (najwyższy %, przy remisie najnowsze).
    public static Result? GetBestResult(int testId, int studentId)
    {
        using var ctx = Ctx();
        var attempts = LoadAttemptsQuery(ctx)
            .Where(a => a.QuizId == testId && a.StudentId == studentId)
            .ToList();
        if (attempts.Count == 0) return null;
        LoadSelectedOptions(ctx, attempts);
        return attempts.Select(ToResultDto)
            .OrderByDescending(r => r.Pct).ThenByDescending(r => r.Id)
            .First();
    }

    // Czy test ma już jakiekolwiek podejście (blokuje edycję pytań).
    public static bool TestHasResults(int testId)
    {
        if (testId == 0) return false;
        using var ctx = Ctx();
        return ctx.QuizAttempts.Any(a => a.QuizId == testId);
    }

    // Redukuje listę podejść do najlepszego na ucznia (statystyki, CSV).
    public static List<Result> BestPerStudent(IEnumerable<Result> results)
        => results.GroupBy(r => r.StudentId)
            .Select(g => g.OrderByDescending(r => r.Pct).ThenByDescending(r => r.Id).First())
            .ToList();

    // Najlepsze podejście na ucznia w obrębie każdego testu (średnie globalne).
    public static List<Result> BestPerStudentPerTest(IEnumerable<Result> results)
        => results.GroupBy(r => new { r.TestId, r.StudentId })
            .Select(g => g.OrderByDescending(r => r.Pct).ThenByDescending(r => r.Id).First())
            .ToList();

    // Zapisuje NOWE podejście ucznia (dopisuje, nie kasuje poprzednich).
    public static void AddAttempt(Result res)
    {
        using var ctx = Ctx();

        // Załaduj opcje pytań (indeks → OptionId)
        var closedQuestions = ctx.Quizzes
            .Where(q => q.Id == res.TestId)
            .Include(q => q.Questions)
            .ThenInclude(q => ((Entities.ClosedQuestion)q).Options)
            .First()
            .Questions.OfType<Entities.ClosedQuestion>()
            .ToDictionary(
                q => q.Id,
                q => q.Options.OrderBy(o => o.Order).ToList()
            );

        var attempt = new Entities.QuizAttempt
        {
            QuizId    = res.TestId,
            StudentId = res.StudentId,
            Score     = res.Score,
            MaxScore  = res.MaxScore,
            Pct       = res.Pct,
            Passed    = res.Passed,
            Pending   = res.Pending,
            Cheated   = res.Cheated,
            TimeUsed  = res.TimeUsed,
            DateTaken = res.DateTaken
        };

        foreach (var (qId, ans) in res.Answers)
        {
            if (ans is string openText)
            {
                // Odpowiedź otwarta
                int? pts = res.Manual?.TryGetValue(qId, out var m) == true ? m : null;
                attempt.Answers.Add(new Entities.OpenAnswer
                {
                    QuestionId    = qId,
                    Text          = openText,
                    PointsAwarded = pts
                });
            }
            else
            {
                // Odpowiedź zamknięta — konwertuj indeksy→OptionId
                var indices = GradingService.AsIndices(ans);
                var ca = new Entities.ClosedAnswer { QuestionId = qId };
                if (closedQuestions.TryGetValue(qId, out var opts))
                {
                    foreach (var idx in indices.Where(i => i >= 0 && i < opts.Count))
                        ca.SelectedOptions.Add(new Entities.SelectedOption { OptionId = opts[idx].Id });
                }
                attempt.Answers.Add(ca);
            }
        }

        ctx.QuizAttempts.Add(attempt);
        ctx.SaveChanges();
        res.Id = attempt.Id;
    }

    // Aktualizuje ocenę istniejącego podejścia (nauczyciel ocenił pytania otwarte).
    // Nie tworzy nowego podejścia — modyfikuje istniejące po Id.
    public static void UpdateGrading(Result res)
    {
        using var ctx = Ctx();
        var attempt = ctx.QuizAttempts
            .Include(a => a.Answers)
            .Include(a => a.Quiz)
            .ThenInclude(q => q.Questions)
            .FirstOrDefault(a => a.Id == res.Id);
        if (attempt == null) return;

        attempt.Score    = res.Score;
        attempt.MaxScore = res.MaxScore;
        attempt.Pct      = res.Pct;
        attempt.Passed   = res.Passed;
        attempt.Pending  = res.Pending;

        // Zapisz ręcznie przyznane punkty na odpowiedziach (każdy typ pytania).
        var manual    = res.Manual ?? new();
        var byQuestion = attempt.Answers.ToDictionary(a => a.QuestionId);
        foreach (var (qId, pts) in manual)
        {
            if (byQuestion.TryGetValue(qId, out var sa))
            {
                sa.PointsAwarded = pts;
            }
            else
            {
                // Pytanie bez wiersza odpowiedzi (np. pominięte) — utwórz placeholder,
                // żeby ręcznie przyznane punkty miały gdzie się zapisać.
                var q = attempt.Quiz?.Questions.FirstOrDefault(x => x.Id == qId);
                Entities.StudentAnswer placeholder = q is Entities.OpenQuestion
                    ? new Entities.OpenAnswer  { QuestionId = qId, Text = "" }
                    : new Entities.ClosedAnswer { QuestionId = qId };
                placeholder.PointsAwarded = pts;
                attempt.Answers.Add(placeholder);
            }
        }

        ctx.SaveChanges();
    }

    // ---------- SAVE / DELETE TEST ----------
    public static void SaveTest(Test t, int teacherId)
    {
        using var ctx = Ctx();

        bool isNew = t.Id == 0;
        // Gdy test ma już podejścia, pytań NIE wolno zmieniać — usunięcie pytań
        // z istniejącymi odpowiedziami uczniów łamałoby klucze obce (Restrict).
        bool hasAttempts = !isNew && ctx.QuizAttempts.Any(a => a.QuizId == t.Id);

        Entities.Quiz quiz;
        if (isNew)
        {
            // Nowy test należy do nauczyciela, który go tworzy.
            quiz = new Entities.Quiz { TeacherId = teacherId };
            ctx.Quizzes.Add(quiz);
        }
        else
        {
            quiz = ctx.Quizzes
                .Include(q => q.Questions)
                .ThenInclude(q => ((Entities.ClosedQuestion)q).Options)
                .Include(q => q.TestGroups)
                .First(q => q.Id == t.Id);

            if (!hasAttempts)
            {
                ctx.Questions.RemoveRange(quiz.Questions);
                quiz.Questions = new List<Entities.Question>();
            }
            // Grupy można zmieniać zawsze (brak FK od odpowiedzi do TestGroup).
            ctx.TestGroups.RemoveRange(quiz.TestGroups);
            quiz.TestGroups = new List<Entities.TestGroup>();
        }

        // Metadane testu są edytowalne zawsze.
        quiz.Title         = t.Title;
        quiz.Subject       = t.Subject;
        quiz.TimeLimit     = t.TimeLimit;
        quiz.PassThreshold = t.PassThreshold;
        quiz.MaxAttempts   = System.Math.Max(1, t.MaxAttempts);
        quiz.IsPublished   = t.IsActive;
        quiz.ColorHex      = t.ColorHex;

        // Pytania budujemy tylko dla nowego testu albo gdy nie ma jeszcze podejść.
        if (isNew || !hasAttempts)
        {
            for (int i = 0; i < t.Questions.Count; i++)
            {
                var q = t.Questions[i];
                Entities.Question entity;
                if (q.Type == QuestionType.Open)
                {
                    entity = new Entities.OpenQuestion
                    {
                        Text = q.Text, Points = q.Points, Order = i, ModelAnswer = q.Sample
                    };
                }
                else
                {
                    var cq = new Entities.ClosedQuestion
                    {
                        Text = q.Text, Points = q.Points, Order = i,
                        IsMultipleChoice = q.Type == QuestionType.Multi,
                        Options = new List<Entities.AnswerOption>()
                    };
                    for (int j = 0; j < q.Options.Count; j++)
                        cq.Options.Add(new Entities.AnswerOption { Text = q.Options[j], IsCorrect = q.Correct.Contains(j), Order = j });
                    entity = cq;
                }
                quiz.Questions.Add(entity);
            }
        }

        foreach (var gid in t.GroupIds)
            quiz.TestGroups.Add(new Entities.TestGroup { GroupId = gid });

        ctx.SaveChanges();
        t.Id = quiz.Id;
    }

    public static void DeleteTest(int id)
    {
        using var ctx = Ctx();
        var quiz = ctx.Quizzes
            .Include(q => q.Attempts)
            .ThenInclude(a => a.Answers)
            .FirstOrDefault(q => q.Id == id);
        if (quiz == null) return;

        // Najpierw odpowiedzi i podejścia (StudentAnswer→Question ma Restrict),
        // potem sam quiz — pytania, opcje i grupy znikną kaskadowo.
        foreach (var a in quiz.Attempts)
            ctx.StudentAnswers.RemoveRange(a.Answers);
        ctx.QuizAttempts.RemoveRange(quiz.Attempts);
        ctx.Quizzes.Remove(quiz);
        ctx.SaveChanges();
    }

    // ---------- HELPERS ----------

    // Bazowe zapytanie o QuizAttempt z załadowanymi odpowiedziami i opcjami pytań
    private static IQueryable<Entities.QuizAttempt> LoadAttemptsQuery(AppDbContext ctx)
        => ctx.QuizAttempts
            .Include(a => a.Answers)
            .Include(a => a.Quiz)
            .ThenInclude(q => q.Questions)
            .ThenInclude(q => ((Entities.ClosedQuestion)q).Options);

    // Ładuje SelectedOptions dla wszystkich ClosedAnswers osobnym zapytaniem
    // (TPH nie wspiera ThenInclude na podtypach we wszystkich wersjach EF)
    private static void LoadSelectedOptions(AppDbContext ctx, IEnumerable<Entities.QuizAttempt> attempts)
    {
        var closedAnswerIds = attempts
            .SelectMany(a => a.Answers.OfType<Entities.ClosedAnswer>())
            .Select(ca => ca.Id)
            .ToList();
        if (closedAnswerIds.Count == 0) return;

        var soList = ctx.SelectedOptions
            .Where(so => closedAnswerIds.Contains(so.ClosedAnswerId))
            .ToList();
        var byAnswerId = soList
            .GroupBy(so => so.ClosedAnswerId)
            .ToDictionary(g => g.Key, g => (ICollection<Entities.SelectedOption>)g.ToList<Entities.SelectedOption>());

        foreach (var attempt in attempts)
            foreach (var ca in attempt.Answers.OfType<Entities.ClosedAnswer>())
                if (byAnswerId.TryGetValue(ca.Id, out var sos))
                    ca.SelectedOptions = sos;
    }

    // ---------- MAPPINGS ----------
    private static User ToDto(Entities.User u) => new User
    {
        Id       = u.Id,
        Name     = u.FullName,
        Login    = u.Username,
        Password = u.Password,
        Role     = u is Entities.Teacher ? "teacher" : "student",
        GroupNames = u is Entities.Student s
            ? s.StudentGroups.Select(sg => sg.Group.Name).OrderBy(n => n).ToList()
            : new List<string>()
    };

    private static Test ToTestDto(Entities.Quiz q) => new Test
    {
        Id            = q.Id,
        Title         = q.Title,
        Subject       = q.Subject,
        TimeLimit     = q.TimeLimit,
        PassThreshold = q.PassThreshold,
        MaxAttempts   = q.MaxAttempts,
        IsActive      = q.IsPublished,
        ColorHex      = q.ColorHex,
        GroupIds      = q.TestGroups.Select(tg => tg.GroupId).ToList(),
        Questions     = q.Questions.OrderBy(qq => qq.Order).Select(ToQuestionDto).ToList()
    };

    private static Question ToQuestionDto(Entities.Question q)
    {
        if (q is Entities.ClosedQuestion cq)
        {
            var opts = cq.Options.OrderBy(o => o.Order).ToList();
            return new Question
            {
                Id       = cq.Id,
                TestId   = cq.QuizId,
                Type     = cq.IsMultipleChoice ? QuestionType.Multi : QuestionType.Single,
                Text     = cq.Text,
                Points   = cq.Points,
                Options  = opts.Select(o => o.Text).ToList(),
                Correct  = opts.Select((o, i) => (o, i)).Where(x => x.o.IsCorrect).Select(x => x.i).ToList(),
                Sample   = "",
                OrderIdx = cq.Order
            };
        }
        var oq = (Entities.OpenQuestion)q;
        return new Question
        {
            Id = oq.Id, TestId = oq.QuizId, Type = QuestionType.Open,
            Text = oq.Text, Points = oq.Points,
            Options = new List<string>(), Correct = new List<int>(),
            Sample = oq.ModelAnswer, OrderIdx = oq.Order
        };
    }

    // Konwertuje QuizAttempt → Result DTO (OptionId→indeks dla odpowiedzi zamkniętych)
    private static Result ToResultDto(Entities.QuizAttempt a)
    {
        var res = new Result
        {
            Id        = a.Id,
            TestId    = a.QuizId,
            StudentId = a.StudentId,
            Score     = a.Score,
            MaxScore  = a.MaxScore,
            Pct       = a.Pct,
            Passed    = a.Passed,
            Pending   = a.Pending,
            Cheated   = a.Cheated,
            TimeUsed  = a.TimeUsed,
            DateTaken = a.DateTaken
        };

        // Słownik: questionId → [orderedOptionId, ...]  (potrzebny do konwersji ID→indeks)
        var optionIdsByQuestion = new Dictionary<int, List<int>>();
        if (a.Quiz != null)
        {
            foreach (var q in a.Quiz.Questions.OfType<Entities.ClosedQuestion>())
                optionIdsByQuestion[q.Id] = q.Options.OrderBy(o => o.Order).Select(o => o.Id).ToList();
        }

        foreach (var sa in a.Answers)
        {
            if (sa is Entities.OpenAnswer oa)
            {
                res.Answers[sa.QuestionId] = oa.Text;
                if (oa.PointsAwarded.HasValue)
                    res.Manual[sa.QuestionId] = oa.PointsAwarded.Value;
            }
            else if (sa is Entities.ClosedAnswer ca)
            {
                if (optionIdsByQuestion.TryGetValue(sa.QuestionId, out var orderedIds))
                {
                    // Konwertuj OptionId → indeks (pozycja w posortowanej liście opcji)
                    var indices = ca.SelectedOptions
                        .Select(so => orderedIds.IndexOf(so.OptionId))
                        .Where(i => i >= 0)
                        .ToList();
                    res.Answers[sa.QuestionId] = indices;
                }
                // Ręcznie nadpisane punkty pytania zamkniętego (jeśli nauczyciel je ustawił).
                if (ca.PointsAwarded.HasValue)
                    res.Manual[sa.QuestionId] = ca.PointsAwarded.Value;
            }
        }

        return res;
    }
}
