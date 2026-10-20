using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kvit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RecurringIncomeRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "ck_recurring_incomes_day_matches_frequency",
                table: "recurring_incomes",
                sql: "(frequency = 'Monthly' AND day_of_month IS NOT NULL AND day_of_week IS NULL) OR (frequency = 'Weekly' AND day_of_week IS NOT NULL AND day_of_month IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_recurring_incomes_title_length",
                table: "recurring_incomes",
                sql: "title IS NULL OR char_length(title) BETWEEN 1 AND 60");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_recurring_incomes_day_matches_frequency",
                table: "recurring_incomes");

            migrationBuilder.DropCheckConstraint(
                name: "ck_recurring_incomes_title_length",
                table: "recurring_incomes");
        }
    }
}
