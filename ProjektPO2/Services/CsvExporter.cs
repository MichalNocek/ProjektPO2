using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ProjektPO2.Services;

// Eksport wyników testu do pliku CSV przy użyciu strumieni (System.IO.StreamWriter).
public static class CsvExporter
{
    // Separator ';' i UTF-8 z BOM, aby polskie znaki i kolumny działały w polskim Excelu.
    public static void ExportTestResults(
        string path, Test test, List<Result> results, IReadOnlyDictionary<int, User> students,
        IReadOnlyCollection<int>? cheatedEver = null)
    {
        using var writer = new StreamWriter(path, false, new UTF8Encoding(true));

        // Nagłówek z metadanymi testu
        writer.WriteLine($"Test;{Esc(test.Title)}");
        writer.WriteLine($"Przedmiot;{Esc(test.Subject)}");
        writer.WriteLine($"Próg zaliczenia;{test.PassThreshold}%");
        writer.WriteLine($"Liczba uczniów;{results.Count}");
        writer.WriteLine();

        // Wiersz nagłówkowy tabeli
        writer.WriteLine("Uczeń;Grupa;Punkty;Maks;Procent;Status;Czas;Data;Uwagi");

        foreach (var r in results)
        {
            students.TryGetValue(r.StudentId, out var st);
            string name   = st?.Name ?? $"#{r.StudentId}";
            string group  = st?.GroupName ?? "";
            bool cheatedOther = !r.Cheated && cheatedEver != null && cheatedEver.Contains(r.StudentId);
            string status = r.Cheated ? "Oszukiwał" : r.Pending ? "Oczekuje na ocenę" : r.Passed ? "Zaliczony" : "Niezaliczony";
            string time   = $"{r.TimeUsed / 60}m {r.TimeUsed % 60}s";
            string notes  = r.Cheated      ? "Wykryto opuszczenie okna testu (próba oszustwa)"
                          : cheatedOther   ? "Próba oszustwa w innym podejściu tego ucznia"
                                           : "";

            writer.WriteLine(string.Join(";", new[]
            {
                Esc(name), Esc(group), r.Score.ToString(), r.MaxScore.ToString(),
                r.Pct + "%", status, time, r.DateTaken.ToString("yyyy-MM-dd HH:mm"), Esc(notes)
            }));
        }
    }

    // Cytowanie wartości zawierających separator, cudzysłów lub nową linię (RFC 4180).
    private static string Esc(string value)
    {
        if (value.Contains(';') || value.Contains('"') || value.Contains('\n'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }
}
