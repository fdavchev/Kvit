using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kvit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BudgetLimits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "budget_limits",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    limit_minor = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_budget_limits", x => x.id);
                    table.CheckConstraint("ck_budget_limits_effective_from_first_day", "extract(day from effective_from) = 1");
                    table.CheckConstraint("ck_budget_limits_limit_minor_valid", "limit_minor IS NULL OR (limit_minor > 0 AND limit_minor <= 999999999999 AND limit_minor % 100 = 0)");
                    table.ForeignKey(
                        name: "fk_budget_limits_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_budget_limits_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_budget_limits_category_id",
                table: "budget_limits",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_budget_limits_user_id_category_id_effective_from",
                table: "budget_limits",
                columns: new[] { "user_id", "category_id", "effective_from" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "budget_limits");
        }
    }
}
