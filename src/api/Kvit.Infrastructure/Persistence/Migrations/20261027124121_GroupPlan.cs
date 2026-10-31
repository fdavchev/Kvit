using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kvit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class GroupPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_activity_events_type",
                table: "activity_events");

            migrationBuilder.AddColumn<long>(
                name: "plan_amount_minor",
                table: "groups",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "plan_currency",
                table: "groups",
                type: "text",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_groups_plan_amount_minor",
                table: "groups",
                sql: "plan_amount_minor > 0 AND plan_amount_minor <= 999999999999");

            migrationBuilder.AddCheckConstraint(
                name: "ck_groups_plan_both_or_neither",
                table: "groups",
                sql: "(plan_amount_minor IS NULL) = (plan_currency IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_groups_plan_currency",
                table: "groups",
                sql: "plan_currency IN ('MKD', 'EUR')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_groups_plan_mkd_whole_denars",
                table: "groups",
                sql: "plan_currency <> 'MKD' OR plan_amount_minor % 100 = 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_activity_events_type",
                table: "activity_events",
                sql: "type IN ('GroupCreated', 'GroupRenamed', 'GroupSettingsChanged', 'InviteLinkReset', 'MemberAdded', 'MemberJoined', 'MemberClaimed', 'ClaimUndone', 'MemberRemoved', 'MemberLeft', 'OwnershipTransferred', 'MemberLetBackIn', 'InviteLinkRestored', 'ExpenseAdded', 'ExpenseEdited', 'ExpenseDeleted', 'ExpenseRestored', 'SettlementRecorded', 'SettlementConfirmed', 'SettlementRejected', 'SettlementCancelled', 'SettlementDeleted', 'SettlementRestored', 'ClosingStarted', 'ClosingConfirmed', 'ClosingObjected', 'ClosingCancelled', 'GroupFinished', 'GroupReopened', 'GroupDeleted', 'GroupRestored', 'GuestLetInAgain', 'GroupPlanSet', 'GroupPlanRemoved')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_groups_plan_amount_minor",
                table: "groups");

            migrationBuilder.DropCheckConstraint(
                name: "ck_groups_plan_both_or_neither",
                table: "groups");

            migrationBuilder.DropCheckConstraint(
                name: "ck_groups_plan_currency",
                table: "groups");

            migrationBuilder.DropCheckConstraint(
                name: "ck_groups_plan_mkd_whole_denars",
                table: "groups");

            migrationBuilder.DropCheckConstraint(
                name: "ck_activity_events_type",
                table: "activity_events");

            migrationBuilder.DropColumn(
                name: "plan_amount_minor",
                table: "groups");

            migrationBuilder.DropColumn(
                name: "plan_currency",
                table: "groups");

            migrationBuilder.AddCheckConstraint(
                name: "ck_activity_events_type",
                table: "activity_events",
                sql: "type IN ('GroupCreated', 'GroupRenamed', 'GroupSettingsChanged', 'InviteLinkReset', 'MemberAdded', 'MemberJoined', 'MemberClaimed', 'ClaimUndone', 'MemberRemoved', 'MemberLeft', 'OwnershipTransferred', 'MemberLetBackIn', 'InviteLinkRestored', 'ExpenseAdded', 'ExpenseEdited', 'ExpenseDeleted', 'ExpenseRestored', 'SettlementRecorded', 'SettlementConfirmed', 'SettlementRejected', 'SettlementCancelled', 'SettlementDeleted', 'SettlementRestored', 'ClosingStarted', 'ClosingConfirmed', 'ClosingObjected', 'ClosingCancelled', 'GroupFinished', 'GroupReopened', 'GroupDeleted', 'GroupRestored', 'GuestLetInAgain')");
        }
    }
}
