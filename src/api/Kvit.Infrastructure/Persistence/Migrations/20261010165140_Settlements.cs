using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kvit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Settlements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_activity_events_type",
                table: "activity_events");

            migrationBuilder.CreateTable(
                name: "settlements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    to_member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount_minor = table.Column<long>(type: "bigint", nullable: false),
                    currency = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    recorded_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    resolved_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resolved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    client_request_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_settlements", x => x.id);
                    table.CheckConstraint("ck_settlements_amount_minor_positive", "amount_minor > 0");
                    table.CheckConstraint("ck_settlements_currency", "currency IN ('MKD', 'EUR')");
                    table.CheckConstraint("ck_settlements_different_members", "from_member_id <> to_member_id");
                    table.CheckConstraint("ck_settlements_status", "status IN ('Pending', 'Confirmed', 'Rejected', 'Cancelled')");
                    table.ForeignKey(
                        name: "fk_settlements_group_members_from_member_id",
                        column: x => x.from_member_id,
                        principalTable: "group_members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_settlements_group_members_to_member_id",
                        column: x => x.to_member_id,
                        principalTable: "group_members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_settlements_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_activity_events_type",
                table: "activity_events",
                sql: "type IN ('GroupCreated', 'GroupRenamed', 'GroupSettingsChanged', 'InviteLinkReset', 'MemberAdded', 'MemberJoined', 'MemberClaimed', 'ClaimUndone', 'MemberRemoved', 'MemberLeft', 'OwnershipTransferred', 'MemberLetBackIn', 'InviteLinkRestored', 'ExpenseAdded', 'ExpenseEdited', 'ExpenseDeleted', 'ExpenseRestored', 'SettlementRecorded', 'SettlementConfirmed', 'SettlementRejected', 'SettlementCancelled', 'SettlementDeleted', 'SettlementRestored', 'ClosingStarted', 'ClosingConfirmed', 'ClosingObjected', 'ClosingCancelled', 'GroupFinished', 'GroupReopened', 'GroupDeleted', 'GroupRestored')");

            migrationBuilder.CreateIndex(
                name: "ix_settlements_client_request_id",
                table: "settlements",
                column: "client_request_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_settlements_from_member_id",
                table: "settlements",
                column: "from_member_id");

            migrationBuilder.CreateIndex(
                name: "ix_settlements_group_id",
                table: "settlements",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_settlements_to_member_id",
                table: "settlements",
                column: "to_member_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "settlements");

            migrationBuilder.DropCheckConstraint(
                name: "ck_activity_events_type",
                table: "activity_events");

            migrationBuilder.AddCheckConstraint(
                name: "ck_activity_events_type",
                table: "activity_events",
                sql: "type IN ('GroupCreated', 'GroupRenamed', 'GroupSettingsChanged', 'InviteLinkReset', 'MemberAdded', 'MemberJoined', 'MemberClaimed', 'ClaimUndone', 'MemberRemoved', 'MemberLeft', 'OwnershipTransferred', 'MemberLetBackIn', 'InviteLinkRestored', 'ExpenseAdded', 'ExpenseEdited', 'ExpenseDeleted', 'ExpenseRestored', 'SettlementRecorded', 'SettlementConfirmed', 'SettlementRejected', 'SettlementCancelled', 'SettlementDeleted', 'ClosingStarted', 'ClosingConfirmed', 'ClosingObjected', 'ClosingCancelled', 'GroupFinished', 'GroupReopened', 'GroupDeleted', 'GroupRestored')");
        }
    }
}
