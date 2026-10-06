using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kvit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Expenses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "expenses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: true),
                    note = table.Column<string>(type: "text", nullable: true),
                    amount_minor = table.Column<long>(type: "bigint", nullable: false),
                    currency = table.Column<string>(type: "text", nullable: false),
                    expense_date = table.Column<DateOnly>(type: "date", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    paid_by_member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    split_type = table.Column<string>(type: "text", nullable: false),
                    mkd_per_eur = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    rate_date = table.Column<DateOnly>(type: "date", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    client_request_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_expenses", x => x.id);
                    table.CheckConstraint("ck_expenses_amount_minor_positive", "amount_minor > 0");
                    table.CheckConstraint("ck_expenses_currency", "currency IN ('MKD', 'EUR')");
                    table.CheckConstraint("ck_expenses_mkd_per_eur_positive", "mkd_per_eur > 0");
                    table.CheckConstraint("ck_expenses_note_length", "char_length(note) BETWEEN 1 AND 500");
                    table.CheckConstraint("ck_expenses_split_type", "split_type IN ('Equal', 'Exact', 'Percentage', 'Shares')");
                    table.CheckConstraint("ck_expenses_title_length", "char_length(title) BETWEEN 1 AND 80");
                    table.ForeignKey(
                        name: "fk_expenses_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_expenses_group_members_paid_by_member_id",
                        column: x => x.paid_by_member_id,
                        principalTable: "group_members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_expenses_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "expense_shares",
                columns: table => new
                {
                    expense_id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    input_value = table.Column<long>(type: "bigint", nullable: false),
                    share_minor = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_expense_shares", x => new { x.expense_id, x.member_id });
                    table.CheckConstraint("ck_expense_shares_input_value_not_negative", "input_value >= 0");
                    table.CheckConstraint("ck_expense_shares_share_minor_not_negative", "share_minor >= 0");
                    table.ForeignKey(
                        name: "fk_expense_shares_expenses_expense_id",
                        column: x => x.expense_id,
                        principalTable: "expenses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_expense_shares_group_members_member_id",
                        column: x => x.member_id,
                        principalTable: "group_members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_expense_shares_member_id",
                table: "expense_shares",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "ix_expenses_category_id",
                table: "expenses",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_expenses_client_request_id",
                table: "expenses",
                column: "client_request_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_expenses_group_id",
                table: "expenses",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_expenses_paid_by_member_id",
                table: "expenses",
                column: "paid_by_member_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "expense_shares");

            migrationBuilder.DropTable(
                name: "expenses");
        }
    }
}
