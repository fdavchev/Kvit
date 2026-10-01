using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kvit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AccountRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_users_normalized_email",
                table: "users");

            migrationBuilder.CreateIndex(
                name: "ix_users_normalized_email",
                table: "users",
                column: "normalized_email",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_users_created_at_set",
                table: "users",
                sql: "created_at > '0002-01-01 00:00:00+00'");

            migrationBuilder.AddCheckConstraint(
                name: "ck_users_display_name_not_empty",
                table: "users",
                sql: "char_length(display_name) >= 1");

            migrationBuilder.AddCheckConstraint(
                name: "ck_users_time_zone_not_empty",
                table: "users",
                sql: "char_length(time_zone) >= 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_users_normalized_email",
                table: "users");

            migrationBuilder.DropCheckConstraint(
                name: "ck_users_created_at_set",
                table: "users");

            migrationBuilder.DropCheckConstraint(
                name: "ck_users_display_name_not_empty",
                table: "users");

            migrationBuilder.DropCheckConstraint(
                name: "ck_users_time_zone_not_empty",
                table: "users");

            migrationBuilder.CreateIndex(
                name: "ix_users_normalized_email",
                table: "users",
                column: "normalized_email");
        }
    }
}
