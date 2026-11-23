using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kvit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SameMomentSafety : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "settlements",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "expenses",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.Sql(KeepOneMergePer("personal_entry_id"));
            migrationBuilder.Sql(KeepOneMergePer("expense_id"));

            migrationBuilder.CreateIndex(
                name: "ix_duplicate_answers_user_id_expense_id_same",
                table: "duplicate_answers",
                columns: new[] { "user_id", "expense_id" },
                unique: true,
                filter: "answer = 'Same'");

            migrationBuilder.CreateIndex(
                name: "ix_duplicate_answers_user_id_personal_entry_id_same",
                table: "duplicate_answers",
                columns: new[] { "user_id", "personal_entry_id" },
                unique: true,
                filter: "answer = 'Same'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_duplicate_answers_user_id_expense_id_same",
                table: "duplicate_answers");

            migrationBuilder.DropIndex(
                name: "ix_duplicate_answers_user_id_personal_entry_id_same",
                table: "duplicate_answers");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "settlements");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "expenses");
        }

        private static string KeepOneMergePer(string column)
        {
            return $"""
                DELETE FROM duplicate_answers
                WHERE id IN (
                    SELECT ranked.id
                    FROM (
                        SELECT answers.id,
                               row_number() OVER (
                                   PARTITION BY answers.user_id, answers.{column}
                                   ORDER BY (
                                       EXISTS (
                                           SELECT 1
                                           FROM personal_entries AS entries
                                           WHERE entries.id = answers.personal_entry_id
                                             AND entries.deleted_at IS NULL)
                                       AND EXISTS (
                                           SELECT 1
                                           FROM expense_shares AS shares
                                           JOIN group_members AS members ON members.id = shares.member_id
                                           JOIN expenses AS shared_expenses ON shared_expenses.id = shares.expense_id
                                           JOIN groups AS expense_groups ON expense_groups.id = shared_expenses.group_id
                                           WHERE shares.expense_id = answers.expense_id
                                             AND shares.counts_in_budget
                                             AND shares.share_minor > 0
                                             AND members.user_id = answers.user_id
                                             AND shared_expenses.deleted_at IS NULL
                                             AND expense_groups.deleted_at IS NULL)
                                   ) DESC, answers.created_at, answers.id) AS place
                        FROM duplicate_answers AS answers
                        WHERE answers.answer = 'Same') AS ranked
                    WHERE ranked.place > 1);
                """;
        }
    }
}
