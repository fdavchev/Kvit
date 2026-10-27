using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kvit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BudgetTotalCurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "budget_total_currency",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_users_budget_total_currency",
                table: "users",
                sql: "budget_total_currency IN ('MKD', 'EUR')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_users_budget_total_currency",
                table: "users");

            migrationBuilder.DropColumn(
                name: "budget_total_currency",
                table: "users");
        }
    }
}
