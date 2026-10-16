using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kvit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class GuestLetInAgain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_activity_events_type",
                table: "activity_events");

            migrationBuilder.AddCheckConstraint(
                name: "ck_activity_events_type",
                table: "activity_events",
                sql: "type IN ('GroupCreated', 'GroupRenamed', 'GroupSettingsChanged', 'InviteLinkReset', 'MemberAdded', 'MemberJoined', 'MemberClaimed', 'ClaimUndone', 'MemberRemoved', 'MemberLeft', 'OwnershipTransferred', 'MemberLetBackIn', 'InviteLinkRestored', 'ExpenseAdded', 'ExpenseEdited', 'ExpenseDeleted', 'ExpenseRestored', 'SettlementRecorded', 'SettlementConfirmed', 'SettlementRejected', 'SettlementCancelled', 'SettlementDeleted', 'SettlementRestored', 'ClosingStarted', 'ClosingConfirmed', 'ClosingObjected', 'ClosingCancelled', 'GroupFinished', 'GroupReopened', 'GroupDeleted', 'GroupRestored', 'GuestLetInAgain')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_activity_events_type",
                table: "activity_events");

            migrationBuilder.AddCheckConstraint(
                name: "ck_activity_events_type",
                table: "activity_events",
                sql: "type IN ('GroupCreated', 'GroupRenamed', 'GroupSettingsChanged', 'InviteLinkReset', 'MemberAdded', 'MemberJoined', 'MemberClaimed', 'ClaimUndone', 'MemberRemoved', 'MemberLeft', 'OwnershipTransferred', 'MemberLetBackIn', 'InviteLinkRestored', 'ExpenseAdded', 'ExpenseEdited', 'ExpenseDeleted', 'ExpenseRestored', 'SettlementRecorded', 'SettlementConfirmed', 'SettlementRejected', 'SettlementCancelled', 'SettlementDeleted', 'SettlementRestored', 'ClosingStarted', 'ClosingConfirmed', 'ClosingObjected', 'ClosingCancelled', 'GroupFinished', 'GroupReopened', 'GroupDeleted', 'GroupRestored')");
        }
    }
}
