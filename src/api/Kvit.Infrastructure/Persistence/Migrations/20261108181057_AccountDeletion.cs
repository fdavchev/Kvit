using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kvit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AccountDeletion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "deletion_name_choice",
                table: "users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deletion_requested_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "purged_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_users_deletion_name_choice",
                table: "users",
                sql: "deletion_name_choice IN ('KeepName', 'FormerMember')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_users_deletion_request_and_choice_together",
                table: "users",
                sql: "(deletion_requested_at IS NULL AND deletion_name_choice IS NULL) OR (deletion_requested_at IS NOT NULL AND deletion_name_choice IS NOT NULL AND char_length(deletion_name_choice) >= 1)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_users_purged_only_after_request",
                table: "users",
                sql: "purged_at IS NULL OR deletion_requested_at IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_users_deletion_name_choice",
                table: "users");

            migrationBuilder.DropCheckConstraint(
                name: "ck_users_deletion_request_and_choice_together",
                table: "users");

            migrationBuilder.DropCheckConstraint(
                name: "ck_users_purged_only_after_request",
                table: "users");

            migrationBuilder.DropColumn(
                name: "deletion_name_choice",
                table: "users");

            migrationBuilder.DropColumn(
                name: "deletion_requested_at",
                table: "users");

            migrationBuilder.DropColumn(
                name: "purged_at",
                table: "users");
        }
    }
}
