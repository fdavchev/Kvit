using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Kvit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OwnerTools : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "suspended_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "admin_closed_at",
                table: "groups",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "admin_actions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    admin_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    target_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_label = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_admin_actions", x => x.id);
                    table.CheckConstraint("ck_admin_actions_action", "action IN ('Suspended', 'Unsuspended', 'GroupClosed', 'GroupReopened', 'NameErased')");
                    table.CheckConstraint("ck_admin_actions_one_target", "(target_user_id IS NULL) <> (target_group_id IS NULL)");
                    table.CheckConstraint("ck_admin_actions_reason_not_empty", "char_length(reason) >= 1");
                    table.ForeignKey(
                        name: "fk_admin_actions_users_admin_user_id",
                        column: x => x.admin_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admin_actions_users_target_user_id",
                        column: x => x.target_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_admin_actions_admin_user_id",
                table: "admin_actions",
                column: "admin_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_admin_actions_created_at",
                table: "admin_actions",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_admin_actions_target_user_id",
                table: "admin_actions",
                column: "target_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_actions");

            migrationBuilder.DropColumn(
                name: "suspended_at",
                table: "users");

            migrationBuilder.DropColumn(
                name: "admin_closed_at",
                table: "groups");
        }
    }
}
