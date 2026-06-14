using System.Windows;
using ProjektPO2.Services;
using ProjektPO2.Views;

namespace ProjektPO2;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            DbRepository.Init();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Nie można połączyć się z bazą danych.\n\n{ex.Message}\n\nUpewnij się, że PostgreSQL działa na localhost:5432 z bazą 'testownik'.",
                "Błąd połączenia",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
            return;
        }

        new LoginWindow().Show();
    }
}
