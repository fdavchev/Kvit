using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kvit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DuplicateAnswers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "duplicate_answers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    personal_entry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    expense_id = table.Column<Guid>(type: "uuid", nullable: false),
                    answer = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_duplicate_answers", x => x.id);
                    table.CheckConstraint("ck_duplicate_answers_answer", "answer IN ('Same', 'New')");
                    table.ForeignKey(
                        name: "fk_duplicate_answers_expenses_expense_id",
                        column: x => x.expense_id,
                        principalTable: "expenses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_duplicate_answers_personal_entries_personal_entry_id",
                        column: x => x.personal_entry_id,
                        principalTable: "personal_entries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_duplicate_answers_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_duplicate_answers_expense_id",
                table: "duplicate_answers",
                column: "expense_id");

            migrationBuilder.CreateIndex(
                name: "ix_duplicate_answers_personal_entry_id",
                table: "duplicate_answers",
                column: "personal_entry_id");

            migrationBuilder.CreateIndex(
                name: "ix_duplicate_answers_user_id_personal_entry_id_expense_id",
                table: "duplicate_answers",
                columns: new[] { "user_id", "personal_entry_id", "expense_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "duplicate_answers");
        }
    }
}
