using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kvit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TermsAcceptance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "terms_accepted_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "terms_version",
                table: "users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_users_terms_version_and_time_together",
                table: "users",
                sql: "(terms_version IS NULL AND terms_accepted_at IS NULL) OR (terms_version IS NOT NULL AND terms_accepted_at IS NOT NULL AND char_length(terms_version) >= 1)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_users_terms_version_and_time_together",
                table: "users");

            migrationBuilder.DropColumn(
                name: "terms_accepted_at",
                table: "users");

            migrationBuilder.DropColumn(
                name: "terms_version",
                table: "users");
        }
    }
}
