using Microsoft.EntityFrameworkCore;
using ProjektPO2.Models;

namespace ProjektPO2.Data;

public class AppDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Teacher> Teachers { get; set; }
    public DbSet<Student> Students { get; set; }
    public DbSet<Group> Groups { get; set; }
    public DbSet<StudentGroup> StudentGroups { get; set; }
    public DbSet<TestGroup> TestGroups { get; set; }
    public DbSet<Quiz> Quizzes { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<ClosedQuestion> ClosedQuestions { get; set; }
    public DbSet<OpenQuestion> OpenQuestions { get; set; }
    public DbSet<AnswerOption> AnswerOptions { get; set; }
    public DbSet<QuizAttempt> QuizAttempts { get; set; }
    public DbSet<StudentAnswer> StudentAnswers { get; set; }
    public DbSet<ClosedAnswer> ClosedAnswers { get; set; }
    public DbSet<OpenAnswer> OpenAnswers { get; set; }
    public DbSet<SelectedOption> SelectedOptions { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=testownik;Username=postgres;Password=123"
        );
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // TPH: User → Teacher / Student
        modelBuilder.Entity<User>()
            .HasDiscriminator<string>("UserType")
            .HasValue<Teacher>("Teacher")
            .HasValue<Student>("Student");

        // Login musi być unikalny globalnie — baza pilnuje, że nie powstaną
        // dwa konta o tym samym loginie (przy logowaniu nie ma więc „losowania" konta).
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        // TPH: Question → ClosedQuestion / OpenQuestion
        modelBuilder.Entity<Question>()
            .HasDiscriminator<string>("QuestionType")
            .HasValue<ClosedQuestion>("Closed")
            .HasValue<OpenQuestion>("Open");

        // StudentGroup composite PK (many-to-many Student ↔ Group)
        modelBuilder.Entity<StudentGroup>()
            .HasKey(sg => new { sg.StudentId, sg.GroupId });

        modelBuilder.Entity<StudentGroup>()
            .HasOne(sg => sg.Student)
            .WithMany(s => s.StudentGroups)
            .HasForeignKey(sg => sg.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<StudentGroup>()
            .HasOne(sg => sg.Group)
            .WithMany(g => g.StudentGroups)
            .HasForeignKey(sg => sg.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        // TestGroup composite PK (many-to-many Quiz ↔ Group)
        modelBuilder.Entity<TestGroup>()
            .HasKey(tg => new { tg.QuizId, tg.GroupId });

        modelBuilder.Entity<TestGroup>()
            .HasOne(tg => tg.Quiz)
            .WithMany(q => q.TestGroups)
            .HasForeignKey(tg => tg.QuizId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TestGroup>()
            .HasOne(tg => tg.Group)
            .WithMany(g => g.TestGroups)
            .HasForeignKey(tg => tg.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        // Teacher → Quizzes
        modelBuilder.Entity<Teacher>()
            .HasMany(t => t.Quizzes)
            .WithOne(q => q.Teacher)
            .HasForeignKey(q => q.TeacherId);

        // Student → Attempts
        modelBuilder.Entity<Student>()
            .HasMany(s => s.Attempts)
            .WithOne(a => a.Student)
            .HasForeignKey(a => a.StudentId);

        // Quiz → Questions
        modelBuilder.Entity<Quiz>()
            .HasMany(q => q.Questions)
            .WithOne(q => q.Quiz)
            .HasForeignKey(q => q.QuizId)
            .OnDelete(DeleteBehavior.Cascade);

        // Quiz → Attempts
        modelBuilder.Entity<Quiz>()
            .HasMany(q => q.Attempts)
            .WithOne(a => a.Quiz)
            .HasForeignKey(a => a.QuizId)
            .OnDelete(DeleteBehavior.Cascade);

        // ClosedQuestion → AnswerOptions
        modelBuilder.Entity<ClosedQuestion>()
            .HasMany(q => q.Options)
            .WithOne(o => o.Question)
            .HasForeignKey(o => o.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Data podejścia jako lokalny czas bez strefy (proste wyświetlanie, bez konwersji UTC).
        modelBuilder.Entity<QuizAttempt>()
            .Property(a => a.DateTaken)
            .HasColumnType("timestamp without time zone");

        // TPH: StudentAnswer → ClosedAnswer / OpenAnswer
        modelBuilder.Entity<StudentAnswer>()
            .HasDiscriminator<string>("AnswerType")
            .HasValue<ClosedAnswer>("Closed")
            .HasValue<OpenAnswer>("Open");

        // StudentAnswer → QuizAttempt (cascade)
        modelBuilder.Entity<StudentAnswer>()
            .HasOne(sa => sa.Attempt)
            .WithMany(a => a.Answers)
            .HasForeignKey(sa => sa.AttemptId)
            .OnDelete(DeleteBehavior.Cascade);

        // StudentAnswer → Question (restrict — kaskada i tak idzie przez Quiz→Attempt→Answer)
        modelBuilder.Entity<StudentAnswer>()
            .HasOne(sa => sa.Question)
            .WithMany()
            .HasForeignKey(sa => sa.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        // ClosedAnswer → SelectedOption (cascade)
        modelBuilder.Entity<ClosedAnswer>()
            .HasMany(ca => ca.SelectedOptions)
            .WithOne(so => so.ClosedAnswer)
            .HasForeignKey(so => so.ClosedAnswerId)
            .OnDelete(DeleteBehavior.Cascade);

        // SelectedOption → AnswerOption (restrict)
        modelBuilder.Entity<SelectedOption>()
            .HasOne(so => so.Option)
            .WithMany()
            .HasForeignKey(so => so.OptionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
