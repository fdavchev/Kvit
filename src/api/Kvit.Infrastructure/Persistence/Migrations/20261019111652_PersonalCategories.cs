using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kvit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PersonalCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_categories_color_not_empty",
                table: "categories");

            migrationBuilder.CreateIndex(
                name: "ix_categories_owner_user_id",
                table: "categories",
                column: "owner_user_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_categories_color",
                table: "categories",
                sql: "color IN ('orange', 'green', 'yellow', 'blue', 'pink', 'purple', 'slate', 'red', 'teal', 'gray')");

            migrationBuilder.AddForeignKey(
                name: "fk_categories_users_owner_user_id",
                table: "categories",
                column: "owner_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_categories_users_owner_user_id",
                table: "categories");

            migrationBuilder.DropIndex(
                name: "ix_categories_owner_user_id",
                table: "categories");

            migrationBuilder.DropCheckConstraint(
                name: "ck_categories_color",
                table: "categories");

            migrationBuilder.AddCheckConstraint(
                name: "ck_categories_color_not_empty",
                table: "categories",
                sql: "char_length(color) >= 1");
        }
    }
}
