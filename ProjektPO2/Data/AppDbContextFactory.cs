using Microsoft.EntityFrameworkCore.Design;

namespace ProjektPO2.Data;

// Fabryka używana przez narzędzia EF Core w trybie projektowym
// (np. polecenia Add-Migration / Update-Database). Pozwala tworzyć kontekst
// bez uruchamiania aplikacji WPF — connection string pochodzi z OnConfiguring.
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args) => new AppDbContext();
}
