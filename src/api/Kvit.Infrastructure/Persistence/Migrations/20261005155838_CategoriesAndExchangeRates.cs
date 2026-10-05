using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Kvit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CategoriesAndExchangeRates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    key = table.Column<string>(type: "text", nullable: true),
                    name = table.Column<string>(type: "text", nullable: true),
                    emoji = table.Column<string>(type: "text", nullable: false),
                    color = table.Column<string>(type: "text", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    archived_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_categories", x => x.id);
                    table.CheckConstraint("ck_categories_built_in_or_custom", "(owner_user_id IS NULL AND key IS NOT NULL AND name IS NULL) OR (owner_user_id IS NOT NULL AND key IS NULL AND name IS NOT NULL)");
                    table.CheckConstraint("ck_categories_color_not_empty", "char_length(color) >= 1");
                    table.CheckConstraint("ck_categories_emoji_not_empty", "char_length(emoji) >= 1");
                });

            migrationBuilder.CreateTable(
                name: "exchange_rates",
                columns: table => new
                {
                    rate_date = table.Column<DateOnly>(type: "date", nullable: false),
                    mkd_per_eur = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    fetched_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_exchange_rates", x => x.rate_date);
                    table.CheckConstraint("ck_exchange_rates_mkd_per_eur_positive", "mkd_per_eur > 0");
                });

            migrationBuilder.InsertData(
                table: "categories",
                columns: new[] { "id", "archived_at", "color", "emoji", "key", "name", "owner_user_id", "sort_order" },
                values: new object[,]
                {
                    { new Guid("c47e9a10-5f3b-4d2e-9a61-000000000001"), null, "orange", "🍽️", "food", null, null, 1 },
                    { new Guid("c47e9a10-5f3b-4d2e-9a61-000000000002"), null, "green", "🛒", "groceries", null, null, 2 },
                    { new Guid("c47e9a10-5f3b-4d2e-9a61-000000000003"), null, "yellow", "🚕", "transport", null, null, 3 },
                    { new Guid("c47e9a10-5f3b-4d2e-9a61-000000000004"), null, "blue", "🏨", "accommodation", null, null, 4 },
                    { new Guid("c47e9a10-5f3b-4d2e-9a61-000000000005"), null, "pink", "🎉", "fun", null, null, 5 },
                    { new Guid("c47e9a10-5f3b-4d2e-9a61-000000000006"), null, "purple", "🛍️", "shopping", null, null, 6 },
                    { new Guid("c47e9a10-5f3b-4d2e-9a61-000000000007"), null, "slate", "🧾", "bills", null, null, 7 },
                    { new Guid("c47e9a10-5f3b-4d2e-9a61-000000000008"), null, "red", "💊", "health", null, null, 8 },
                    { new Guid("c47e9a10-5f3b-4d2e-9a61-000000000009"), null, "teal", "🎁", "gifts", null, null, 9 },
                    { new Guid("c47e9a10-5f3b-4d2e-9a61-000000000010"), null, "gray", "📦", "other", null, null, 10 }
                });

            migrationBuilder.InsertData(
                table: "exchange_rates",
                columns: new[] { "rate_date", "fetched_at", "mkd_per_eur" },
                values: new object[] { new DateOnly(2026, 9, 24), new DateTimeOffset(new DateTime(2026, 9, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 61.5610m });

            migrationBuilder.CreateIndex(
                name: "ix_categories_key",
                table: "categories",
                column: "key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "categories");

            migrationBuilder.DropTable(
                name: "exchange_rates");
        }
    }
}
