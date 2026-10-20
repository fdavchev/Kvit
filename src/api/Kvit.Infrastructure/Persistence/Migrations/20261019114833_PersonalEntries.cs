using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kvit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PersonalEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "personal_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "text", nullable: false),
                    amount_minor = table.Column<long>(type: "bigint", nullable: false),
                    currency = table.Column<string>(type: "text", nullable: false),
                    mkd_per_eur = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    rate_date = table.Column<DateOnly>(type: "date", nullable: false),
                    entry_date = table.Column<DateOnly>(type: "date", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    note = table.Column<string>(type: "text", nullable: true),
                    client_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_personal_entries", x => x.id);
                    table.CheckConstraint("ck_personal_entries_amount_minor_positive", "amount_minor > 0");
                    table.CheckConstraint("ck_personal_entries_currency", "currency IN ('MKD', 'EUR')");
                    table.CheckConstraint("ck_personal_entries_kind", "kind IN ('Spending')");
                    table.CheckConstraint("ck_personal_entries_mkd_per_eur_positive", "mkd_per_eur > 0");
                    table.CheckConstraint("ck_personal_entries_note_length", "char_length(note) BETWEEN 1 AND 500");
                    table.CheckConstraint("ck_personal_entries_spending_has_category", "kind <> 'Spending' OR category_id IS NOT NULL");
                    table.ForeignKey(
                        name: "fk_personal_entries_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_personal_entries_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_personal_entries_category_id",
                table: "personal_entries",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_personal_entries_client_request_id",
                table: "personal_entries",
                column: "client_request_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_personal_entries_user_id_entry_date",
                table: "personal_entries",
                columns: new[] { "user_id", "entry_date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "personal_entries");
        }
    }
}
