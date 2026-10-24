using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kvit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Income : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_personal_entries_kind",
                table: "personal_entries");

            migrationBuilder.AddColumn<Guid>(
                name: "recurring_income_id",
                table: "personal_entries",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "recurring_incomes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount_minor = table.Column<long>(type: "bigint", nullable: false),
                    currency = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: true),
                    frequency = table.Column<string>(type: "text", nullable: false),
                    day_of_month = table.Column<int>(type: "integer", nullable: true),
                    day_of_week = table.Column<int>(type: "integer", nullable: true),
                    starts_on = table.Column<DateOnly>(type: "date", nullable: false),
                    ends_on = table.Column<DateOnly>(type: "date", nullable: true),
                    client_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    stopped_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_recurring_incomes", x => x.id);
                    table.CheckConstraint("ck_recurring_incomes_amount_minor_positive", "amount_minor > 0");
                    table.CheckConstraint("ck_recurring_incomes_currency", "currency IN ('MKD', 'EUR')");
                    table.CheckConstraint("ck_recurring_incomes_day_of_month", "day_of_month IS NULL OR day_of_month BETWEEN 1 AND 31");
                    table.CheckConstraint("ck_recurring_incomes_day_of_week", "day_of_week IS NULL OR day_of_week BETWEEN 0 AND 6");
                    table.CheckConstraint("ck_recurring_incomes_ends_on_not_before_starts_on", "ends_on IS NULL OR ends_on >= starts_on");
                    table.CheckConstraint("ck_recurring_incomes_frequency", "frequency IN ('Monthly', 'Weekly')");
                    table.ForeignKey(
                        name: "fk_recurring_incomes_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_personal_entries_recurring_income_id_entry_date",
                table: "personal_entries",
                columns: new[] { "recurring_income_id", "entry_date" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_personal_entries_kind",
                table: "personal_entries",
                sql: "kind IN ('Spending', 'Income')");

            migrationBuilder.CreateIndex(
                name: "ix_recurring_incomes_client_request_id",
                table: "recurring_incomes",
                column: "client_request_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_recurring_incomes_user_id",
                table: "recurring_incomes",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_personal_entries_recurring_incomes_recurring_income_id",
                table: "personal_entries",
                column: "recurring_income_id",
                principalTable: "recurring_incomes",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_personal_entries_recurring_incomes_recurring_income_id",
                table: "personal_entries");

            migrationBuilder.DropTable(
                name: "recurring_incomes");

            migrationBuilder.DropIndex(
                name: "ix_personal_entries_recurring_income_id_entry_date",
                table: "personal_entries");

            migrationBuilder.DropCheckConstraint(
                name: "ck_personal_entries_kind",
                table: "personal_entries");

            migrationBuilder.DropColumn(
                name: "recurring_income_id",
                table: "personal_entries");

            migrationBuilder.AddCheckConstraint(
                name: "ck_personal_entries_kind",
                table: "personal_entries",
                sql: "kind IN ('Spending')");
        }
    }
}
