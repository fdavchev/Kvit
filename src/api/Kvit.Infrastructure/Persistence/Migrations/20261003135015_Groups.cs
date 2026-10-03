using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kvit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Groups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "groups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    emoji = table.Column<string>(type: "text", nullable: false),
                    default_currency = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    closing_started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    finished_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    invite_token = table.Column<string>(type: "text", nullable: false),
                    invite_token_created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    previous_invite_token = table.Column<string>(type: "text", nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_groups", x => x.id);
                    table.CheckConstraint("ck_groups_default_currency", "default_currency IN ('MKD', 'EUR')");
                    table.CheckConstraint("ck_groups_kind", "kind IN ('OneBill', 'Group')");
                    table.CheckConstraint("ck_groups_name_not_empty", "char_length(name) >= 1");
                    table.CheckConstraint("ck_groups_status", "status IN ('Open', 'Closing', 'Finished')");
                    table.ForeignKey(
                        name: "fk_groups_users_owner_user_id",
                        column: x => x.owner_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "activity_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    expense_id = table.Column<Guid>(type: "uuid", nullable: true),
                    settlement_id = table.Column<Guid>(type: "uuid", nullable: true),
                    member_id = table.Column<Guid>(type: "uuid", nullable: true),
                    changes = table.Column<string>(type: "jsonb", nullable: true),
                    data = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_activity_events", x => x.id);
                    table.CheckConstraint("ck_activity_events_type", "type IN ('GroupCreated', 'GroupRenamed', 'GroupSettingsChanged', 'InviteLinkReset', 'MemberAdded', 'MemberJoined', 'MemberClaimed', 'ClaimUndone', 'MemberRemoved', 'MemberLeft', 'OwnershipTransferred', 'MemberLetBackIn', 'InviteLinkRestored', 'ExpenseAdded', 'ExpenseEdited', 'ExpenseDeleted', 'ExpenseRestored', 'SettlementRecorded', 'SettlementConfirmed', 'SettlementRejected', 'SettlementCancelled', 'SettlementDeleted', 'ClosingStarted', 'ClosingConfirmed', 'ClosingObjected', 'ClosingCancelled', 'GroupFinished', 'GroupReopened', 'GroupDeleted', 'GroupRestored')");
                    table.ForeignKey(
                        name: "fk_activity_events_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "group_members",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    joined_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    added_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    claimed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    removed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    removed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    end_kind = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_group_members", x => x.id);
                    table.CheckConstraint("ck_group_members_end_kind", "end_kind IN ('Left', 'Removed', 'SetAside')");
                    table.CheckConstraint("ck_group_members_name_not_empty", "char_length(name) >= 1");
                    table.CheckConstraint("ck_group_members_removed_at_with_end_kind", "(removed_at IS NULL) = (end_kind IS NULL)");
                    table.ForeignKey(
                        name: "fk_group_members_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_group_members_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_activity_events_group_id_created_at",
                table: "activity_events",
                columns: new[] { "group_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_group_members_group_id",
                table: "group_members",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_group_members_group_id_user_id",
                table: "group_members",
                columns: new[] { "group_id", "user_id" },
                unique: true,
                filter: "user_id IS NOT NULL AND removed_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_group_members_user_id",
                table: "group_members",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_groups_invite_token",
                table: "groups",
                column: "invite_token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_groups_owner_user_id",
                table: "groups",
                column: "owner_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "activity_events");

            migrationBuilder.DropTable(
                name: "group_members");

            migrationBuilder.DropTable(
                name: "groups");
        }
    }
}
