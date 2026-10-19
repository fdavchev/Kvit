using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kvit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class GuestUpgradedUsageEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_usage_events_type",
                table: "usage_events");

            migrationBuilder.AddCheckConstraint(
                name: "ck_usage_events_type",
                table: "usage_events",
                sql: "type IN ('SignedUp', 'Active', 'JoinedViaInvite', 'GroupCreated', 'ExpenseAdded', 'SettlementConfirmed', 'GuestUpgraded')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_usage_events_type",
                table: "usage_events");

            migrationBuilder.AddCheckConstraint(
                name: "ck_usage_events_type",
                table: "usage_events",
                sql: "type IN ('SignedUp', 'Active', 'JoinedViaInvite', 'GroupCreated', 'ExpenseAdded', 'SettlementConfirmed')");
        }
    }
}
