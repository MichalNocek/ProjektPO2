using ProjektPO2.Models;

namespace ProjektPO2.Data;

public static class DbSeeder
{
    public static void Seed(AppDbContext ctx)
    {
        if (ctx.Users.Any()) return;

        // Teacher
        var teacher = new Teacher
        {
            Username = "m.dabrowski",
            Password = Services.PasswordHash.Hash("demo1234"),
            FirstName = "Marek",
            LastName = "Dąbrowski"
        };
        ctx.Teachers.Add(teacher);

        // Students
        var studentData = new[]
        {
            ("Anna",   "Kowalska",     "anna.k"),
            ("Piotr",  "Nowak",        "piotr.n"),
            ("Maria",  "Wiśniewska",   "maria.w"),
            ("Jakub",  "Lewandowski",  "jakub.l"),
            ("Zofia",  "Wójcik",       "zofia.w"),
            ("Kacper", "Kamiński",     "kacper.k"),
            ("Julia",  "Zielińska",    "julia.z"),
            ("Filip",  "Szymański",    "filip.s"),
        };
        var students = new List<Student>();
        foreach (var (fn, ln, un) in studentData)
        {
            var s = new Student
            {
                Username  = un,
                Password  = Services.PasswordHash.Hash("demo1234"),
                FirstName = fn,
                LastName  = ln
            };
            students.Add(s);
            ctx.Students.Add(s);
        }

        // Groups
        var g3A = new Group { Name = "3A", Description = "Klasa 3A — profil mat-fiz" };
        var g3B = new Group { Name = "3B", Description = "Klasa 3B — profil biol-chem" };
        var g2C = new Group { Name = "2C", Description = "Klasa 2C — profil ogólny" };
        ctx.Groups.AddRange(g3A, g3B, g2C);

        ctx.SaveChanges();

        // Student ↔ Group assignments (many-to-many); Piotr i Zofia są w 2 grupach (demo)
        var sgData = new[]
        {
            (students[0], g3A), // Anna → 3A
            (students[1], g3A), // Piotr → 3A
            (students[1], g2C), // Piotr → 2C (też)
            (students[2], g3A), // Maria → 3A
            (students[3], g3A), // Jakub → 3A
            (students[4], g3A), // Zofia → 3A (też)
            (students[4], g3B), // Zofia → 3B
            (students[5], g3B), // Kacper → 3B
            (students[6], g3B), // Julia → 3B
            (students[7], g2C), // Filip → 2C
        };
        foreach (var (student, group) in sgData)
            ctx.StudentGroups.Add(new StudentGroup { StudentId = student.Id, GroupId = group.Id });

        ctx.SaveChanges();

        // Test 1 — Funkcja kwadratowa (dla grupy 3A)
        var t1 = new Quiz
        {
            Title = "Funkcja kwadratowa",
            Subject = "Matematyka",
            TimeLimit = 20,
            PassThreshold = 50,
            ColorHex = "#2563EB",
            TeacherId = teacher.Id,
            IsPublished = true,
            TestGroups = new List<TestGroup> { new() { GroupId = g3A.Id } },
            Questions = new List<Question>
            {
                new ClosedQuestion
                {
                    Text = "Jaka jest postać kanoniczna funkcji kwadratowej?",
                    Points = 1, Order = 0, IsMultipleChoice = false,
                    Options = new List<AnswerOption>
                    {
                        new() { Text = "y = ax² + bx + c", IsCorrect = false, Order = 0 },
                        new() { Text = "y = a(x − p)² + q", IsCorrect = true,  Order = 1 },
                        new() { Text = "y = a(x − x₁)(x − x₂)", IsCorrect = false, Order = 2 },
                        new() { Text = "y = ax + b", IsCorrect = false, Order = 3 },
                    }
                },
                new ClosedQuestion
                {
                    Text = "Ile miejsc zerowych ma funkcja, gdy Δ < 0?",
                    Points = 1, Order = 1, IsMultipleChoice = false,
                    Options = new List<AnswerOption>
                    {
                        new() { Text = "0", IsCorrect = true,  Order = 0 },
                        new() { Text = "1", IsCorrect = false, Order = 1 },
                        new() { Text = "2", IsCorrect = false, Order = 2 },
                        new() { Text = "Nieskończenie wiele", IsCorrect = false, Order = 3 },
                    }
                },
                new ClosedQuestion
                {
                    Text = "Które stwierdzenia są prawdziwe dla y = ax², gdy a > 0?",
                    Points = 2, Order = 2, IsMultipleChoice = true,
                    Options = new List<AnswerOption>
                    {
                        new() { Text = "Ramiona skierowane do góry", IsCorrect = true,  Order = 0 },
                        new() { Text = "Wierzchołek w (0,0)",        IsCorrect = true,  Order = 1 },
                        new() { Text = "Malejąca na całej dziedzinie",IsCorrect = false, Order = 2 },
                        new() { Text = "Oś symetrii x = 0",          IsCorrect = true,  Order = 3 },
                    }
                },
                new OpenQuestion
                {
                    Text = "Wyznacz współrzędne wierzchołka y = x² − 4x + 3. Format (p, q).",
                    Points = 2, Order = 3, ModelAnswer = "(2, -1)"
                },
            }
        };
        ctx.Quizzes.Add(t1);

        // Test 2 — II wojna światowa (dla 3A i 3B)
        var t2 = new Quiz
        {
            Title = "II wojna światowa — podstawy",
            Subject = "Historia",
            TimeLimit = 25,
            PassThreshold = 60,
            ColorHex = "#D97706",
            TeacherId = teacher.Id,
            IsPublished = true,
            TestGroups = new List<TestGroup> { new() { GroupId = g3A.Id }, new() { GroupId = g3B.Id } },
            Questions = new List<Question>
            {
                new ClosedQuestion
                {
                    Text = "W którym roku rozpoczęła się II wojna światowa?",
                    Points = 1, Order = 0, IsMultipleChoice = false,
                    Options = new List<AnswerOption>
                    {
                        new() { Text = "1938", IsCorrect = false, Order = 0 },
                        new() { Text = "1939", IsCorrect = true,  Order = 1 },
                        new() { Text = "1940", IsCorrect = false, Order = 2 },
                        new() { Text = "1941", IsCorrect = false, Order = 3 },
                    }
                },
                new ClosedQuestion
                {
                    Text = "Które państwa należały do Państw Osi?",
                    Points = 2, Order = 1, IsMultipleChoice = true,
                    Options = new List<AnswerOption>
                    {
                        new() { Text = "Niemcy",         IsCorrect = true,  Order = 0 },
                        new() { Text = "Włochy",         IsCorrect = true,  Order = 1 },
                        new() { Text = "Wielka Brytania",IsCorrect = false, Order = 2 },
                        new() { Text = "Japonia",        IsCorrect = true,  Order = 3 },
                    }
                },
                new OpenQuestion
                {
                    Text = "Podaj datę wybuchu powstania warszawskiego.",
                    Points = 2, Order = 2, ModelAnswer = "1 sierpnia 1944"
                },
            }
        };
        ctx.Quizzes.Add(t2);

        // Test 3 — Biologia (dla 3B)
        var t3 = new Quiz
        {
            Title = "Komórka i jej budowa",
            Subject = "Biologia",
            TimeLimit = 15,
            PassThreshold = 50,
            ColorHex = "#15A34A",
            TeacherId = teacher.Id,
            IsPublished = true,
            TestGroups = new List<TestGroup> { new() { GroupId = g3B.Id } },
            Questions = new List<Question>
            {
                new ClosedQuestion
                {
                    Text = "Który organellum produkuje energię (ATP)?",
                    Points = 1, Order = 0, IsMultipleChoice = false,
                    Options = new List<AnswerOption>
                    {
                        new() { Text = "Jądro",           IsCorrect = false, Order = 0 },
                        new() { Text = "Mitochondrium",   IsCorrect = true,  Order = 1 },
                        new() { Text = "Rybosom",         IsCorrect = false, Order = 2 },
                        new() { Text = "Aparat Golgiego", IsCorrect = false, Order = 3 },
                    }
                },
                new ClosedQuestion
                {
                    Text = "Co występuje w komórce roślinnej, a nie ma w zwierzęcej?",
                    Points = 2, Order = 1, IsMultipleChoice = true,
                    Options = new List<AnswerOption>
                    {
                        new() { Text = "Ściana komórkowa",  IsCorrect = true,  Order = 0 },
                        new() { Text = "Chloroplasty",      IsCorrect = true,  Order = 1 },
                        new() { Text = "Mitochondria",      IsCorrect = false, Order = 2 },
                        new() { Text = "Wakuola centralna", IsCorrect = true,  Order = 3 },
                    }
                },
            }
        };
        ctx.Quizzes.Add(t3);
        ctx.SaveChanges();
    }
}
