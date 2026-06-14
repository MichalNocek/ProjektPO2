using System;
using System.Windows;

namespace ProjektPO2.Services;

// Centralna obsługa wyjątków dla operacji na bazie danych / IO.
// Łapie wyjątek, pokazuje czytelny komunikat i nie pozwala wywrócić aplikacji.
public static class Safe
{
    // Wykonuje operację bez zwracania wartości. Zwraca true gdy się powiodła.
    public static bool Run(Action op, string action)
    {
        try
        {
            op();
            return true;
        }
        catch (Exception ex)
        {
            Show(action, ex);
            return false;
        }
    }

    // Wykonuje operację zwracającą wartość. Przy błędzie zwraca fallback.
    public static T Get<T>(Func<T> op, T fallback, string action)
    {
        try
        {
            return op();
        }
        catch (Exception ex)
        {
            Show(action, ex);
            return fallback;
        }
    }

    private static void Show(string action, Exception ex)
        => MessageBox.Show(
            $"Nie udało się {action}.\n\nSzczegóły błędu:\n{ex.Message}",
            "Błąd operacji",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
}
