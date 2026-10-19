using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kvit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class GuestAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_guest",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddCheckConstraint(
                name: "ck_users_guest_has_no_email_or_password",
                table: "users",
                sql: "NOT is_guest OR (email IS NULL AND normalized_email IS NULL AND password_hash IS NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_users_guest_has_no_email_or_password",
                table: "users");

            migrationBuilder.DropColumn(
                name: "is_guest",
                table: "users");
        }
    }
}
