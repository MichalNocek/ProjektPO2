using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace ProjektPO2.Services;

public static class GradingService
{
    public static List<int> AsIndices(object? ans)
    {
        if (ans is List<int> li) return li;
        if (ans is int i) return new List<int> { i };
        if (ans is JsonElement je)
        {
            if (je.ValueKind == JsonValueKind.Array)
                return je.EnumerateArray().Select(x => x.GetInt32()).ToList();
            if (je.ValueKind == JsonValueKind.Number)
                return new List<int> { je.GetInt32() };
        }
        return new List<int>();
    }

    public static string AsText(object? ans)
    {
        if (ans is string s) return s;
        if (ans is JsonElement je && je.ValueKind == JsonValueKind.String)
            return je.GetString() ?? "";
        return ans?.ToString() ?? "";
    }

    public static (int pts, bool ok, bool partial, bool pending) Grade(
        Question q, object? ans, IReadOnlyDictionary<int, int>? manual = null)
    {
        // Ręczne nadpisanie punktów ma pierwszeństwo i dotyczy KAŻDEGO typu pytania
        // (nauczyciel może ręcznie przyznać punkty także w pytaniach zamkniętych).
        if (manual != null && manual.TryGetValue(q.Id, out var mo))
            return (mo, mo >= q.Points, mo > 0 && mo < q.Points, false);

        switch (q.Type)
        {
            case QuestionType.Single:
            {
                var idx = AsIndices(ans);
                bool ok = idx.Count > 0 && q.Correct.Contains(idx[0]);
                return (ok ? q.Points : 0, ok, false, false);
            }
            case QuestionType.Multi:
            {
                var chosen = AsIndices(ans);
                int good = chosen.Count(i => q.Correct.Contains(i));
                int bad = chosen.Count(i => !q.Correct.Contains(i));
                double raw = q.Correct.Count == 0 ? 0 : Math.Max(0, good - bad) / (double)q.Correct.Count;
                int pts = (int)Math.Round(raw * q.Points);
                bool full = pts == q.Points && q.Points > 0;
                return (pts, full, pts > 0 && !full, false);
            }
            case QuestionType.Open:
                // Pytanie otwarte bez ręcznej oceny czeka na nauczyciela.
                return (0, false, false, true);
        }
        return (0, false, false, false);
    }

    public static Result NewResult(Test test, int studentId, Dictionary<int, object> answers, int timeUsed)
    {
        var r = new Result
        {
            TestId = test.Id,
            StudentId = studentId,
            Answers = answers,
            Manual = new(),
            TimeUsed = timeUsed,
            DateTaken = DateTime.Now
        };
        Recompute(test, r);
        return r;
    }

    public static void Recompute(Test test, Result r)
    {
        int score = 0, max = 0;
        bool pending = false;
        r.Manual ??= new();
        foreach (var q in test.Questions)
        {
            var g = Grade(q, r.Answers.TryGetValue(q.Id, out var a) ? a : null, r.Manual);
            if (g.pending) pending = true;
            else score += g.pts;
            max += q.Points;
        }
        r.Score = score;
        r.MaxScore = max;
        r.Pct = max == 0 ? 0 : (int)Math.Round(score * 100.0 / max);
        r.Passed = r.Pct >= test.PassThreshold;
        r.Pending = pending;
    }

    public static List<Question> PendingOpen(Test test, Result r)
        => test.Questions.Where(q =>
            q.Type == QuestionType.Open &&
            !(r.Manual != null && r.Manual.ContainsKey(q.Id))).ToList();

    public static int Correctness(Test test, Question q, List<Result> results)
    {
        if (results.Count == 0 || q.Points == 0) return 0;
        double sum = 0;
        foreach (var r in results)
        {
            r.Answers.TryGetValue(q.Id, out var a);
            sum += Grade(q, a, r.Manual).pts / (double)q.Points;
        }
        return (int)Math.Round(sum / results.Count * 100);
    }
}
