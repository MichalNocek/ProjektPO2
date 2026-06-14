using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjektPO2.Migrations
{
    /// <inheritdoc />
    public partial class DateTakenToDateTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Konwersja istniejących wartości tekstowych ("yyyy-MM-dd") na timestamp.
            // Klauzula USING jest konieczna — PostgreSQL nie rzutuje text→timestamp niejawnie.
            migrationBuilder.Sql(
                "ALTER TABLE \"QuizAttempts\" " +
                "ALTER COLUMN \"DateTaken\" TYPE timestamp without time zone " +
                "USING \"DateTaken\"::timestamp without time zone;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE \"QuizAttempts\" " +
                "ALTER COLUMN \"DateTaken\" TYPE text " +
                "USING to_char(\"DateTaken\", 'YYYY-MM-DD\"T\"HH24:MI:SS');");
        }
    }
}
