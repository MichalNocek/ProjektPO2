using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjektPO2.Migrations
{
    /// <inheritdoc />
    public partial class HashPasswords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Zahashuj istniejące hasła zapisane otwartym tekstem.
            // Format identyczny jak w PasswordHash.Hash w C# (SHA-256 → hex małymi literami),
            // więc konta demo działają dalej bez kasowania bazy.
            // Warunek pomija wartości, które już wyglądają jak hash (64 znaki hex).
            migrationBuilder.Sql(
                "UPDATE \"Users\" " +
                "SET \"Password\" = encode(sha256(convert_to(\"Password\", 'UTF8')), 'hex') " +
                "WHERE \"Password\" !~ '^[0-9a-f]{64}$';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Hasha nie da się odwrócić — brak operacji wstecznej.
        }
    }
}
