using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kvit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PersonalEntryOriginRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "ck_personal_entries_generated_is_income",
                table: "personal_entries",
                sql: "recurring_income_id IS NULL OR kind = 'Income'");

            migrationBuilder.AddCheckConstraint(
                name: "ck_personal_entries_has_origin",
                table: "personal_entries",
                sql: "client_request_id IS NOT NULL OR recurring_income_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_personal_entries_income_has_no_category",
                table: "personal_entries",
                sql: "kind <> 'Income' OR category_id IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_personal_entries_generated_is_income",
                table: "personal_entries");

            migrationBuilder.DropCheckConstraint(
                name: "ck_personal_entries_has_origin",
                table: "personal_entries");

            migrationBuilder.DropCheckConstraint(
                name: "ck_personal_entries_income_has_no_category",
                table: "personal_entries");
        }
    }
}
