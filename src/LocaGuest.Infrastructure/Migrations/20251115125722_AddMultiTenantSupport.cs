using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocaGuest.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiTenantSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Subscriptions_Plans_PlanId",
                table: "Subscriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_UsageEvents_Subscriptions_SubscriptionId",
                table: "UsageEvents");

            migrationBuilder.DropIndex(
                name: "IX_user_settings_UserId",
                table: "user_settings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Subscriptions",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_rentability_scenarios_UserId",
                table: "rentability_scenarios");

            migrationBuilder.DropIndex(
                name: "IX_rentability_scenarios_UserId_IsBase",
                table: "rentability_scenarios");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Plans",
                table: "Plans");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UsageEvents",
                table: "UsageEvents");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UsageAggregates",
                table: "UsageAggregates");

            migrationBuilder.RenameTable(
                name: "Subscriptions",
                newName: "subscriptions");

            migrationBuilder.RenameTable(
                name: "Plans",
                newName: "plans");

            migrationBuilder.RenameTable(
                name: "UsageEvents",
                newName: "usage_events");

            migrationBuilder.RenameTable(
                name: "UsageAggregates",
                newName: "usage_aggregates");

            migrationBuilder.RenameIndex(
                name: "IX_Subscriptions_PlanId",
                table: "subscriptions",
                newName: "IX_subscriptions_PlanId");

            migrationBuilder.RenameIndex(
                name: "IX_UsageEvents_SubscriptionId",
                table: "usage_events",
                newName: "IX_usage_events_SubscriptionId");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "user_settings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "tenants",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "subscriptions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "ScenarioComment",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "scenario_versions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "scenario_shares",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "rentability_scenarios",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "properties",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "plans",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "plans",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "contracts",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "RenterTenantId",
                table: "contracts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "usage_events",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "usage_aggregates",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_subscriptions",
                table: "subscriptions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_plans",
                table: "plans",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_usage_events",
                table: "usage_events",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_usage_aggregates",
                table: "usage_aggregates",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_user_settings_TenantId_UserId",
                table: "user_settings",
                columns: new[] { "TenantId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenants_TenantId",
                table: "tenants",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_StripeSubscriptionId",
                table: "subscriptions",
                column: "StripeSubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_TenantId_UserId",
                table: "subscriptions",
                columns: new[] { "TenantId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_rentability_scenarios_TenantId",
                table: "rentability_scenarios",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_rentability_scenarios_TenantId_UserId",
                table: "rentability_scenarios",
                columns: new[] { "TenantId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_rentability_scenarios_TenantId_UserId_IsBase",
                table: "rentability_scenarios",
                columns: new[] { "TenantId", "UserId", "IsBase" });

            migrationBuilder.CreateIndex(
                name: "IX_properties_TenantId",
                table: "properties",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_plans_Code",
                table: "plans",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_contracts_RenterTenantId",
                table: "contracts",
                column: "RenterTenantId");

            migrationBuilder.CreateIndex(
                name: "IX_contracts_TenantId",
                table: "contracts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_usage_events_TenantId_SubscriptionId",
                table: "usage_events",
                columns: new[] { "TenantId", "SubscriptionId" });

            migrationBuilder.CreateIndex(
                name: "IX_usage_aggregates_TenantId_UserId_Dimension_PeriodYear_Perio~",
                table: "usage_aggregates",
                columns: new[] { "TenantId", "UserId", "Dimension", "PeriodYear", "PeriodMonth" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_subscriptions_plans_PlanId",
                table: "subscriptions",
                column: "PlanId",
                principalTable: "plans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_usage_events_subscriptions_SubscriptionId",
                table: "usage_events",
                column: "SubscriptionId",
                principalTable: "subscriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_subscriptions_plans_PlanId",
                table: "subscriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_usage_events_subscriptions_SubscriptionId",
                table: "usage_events");

            migrationBuilder.DropIndex(
                name: "IX_user_settings_TenantId_UserId",
                table: "user_settings");

            migrationBuilder.DropIndex(
                name: "IX_tenants_TenantId",
                table: "tenants");

            migrationBuilder.DropPrimaryKey(
                name: "PK_subscriptions",
                table: "subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_subscriptions_StripeSubscriptionId",
                table: "subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_subscriptions_TenantId_UserId",
                table: "subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_rentability_scenarios_TenantId",
                table: "rentability_scenarios");

            migrationBuilder.DropIndex(
                name: "IX_rentability_scenarios_TenantId_UserId",
                table: "rentability_scenarios");

            migrationBuilder.DropIndex(
                name: "IX_rentability_scenarios_TenantId_UserId_IsBase",
                table: "rentability_scenarios");

            migrationBuilder.DropIndex(
                name: "IX_properties_TenantId",
                table: "properties");

            migrationBuilder.DropPrimaryKey(
                name: "PK_plans",
                table: "plans");

            migrationBuilder.DropIndex(
                name: "IX_plans_Code",
                table: "plans");

            migrationBuilder.DropIndex(
                name: "IX_contracts_RenterTenantId",
                table: "contracts");

            migrationBuilder.DropIndex(
                name: "IX_contracts_TenantId",
                table: "contracts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_usage_events",
                table: "usage_events");

            migrationBuilder.DropIndex(
                name: "IX_usage_events_TenantId_SubscriptionId",
                table: "usage_events");

            migrationBuilder.DropPrimaryKey(
                name: "PK_usage_aggregates",
                table: "usage_aggregates");

            migrationBuilder.DropIndex(
                name: "IX_usage_aggregates_TenantId_UserId_Dimension_PeriodYear_Perio~",
                table: "usage_aggregates");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "user_settings");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ScenarioComment");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "scenario_versions");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "scenario_shares");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "rentability_scenarios");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "plans");

            migrationBuilder.DropColumn(
                name: "RenterTenantId",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "usage_events");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "usage_aggregates");

            migrationBuilder.RenameTable(
                name: "subscriptions",
                newName: "Subscriptions");

            migrationBuilder.RenameTable(
                name: "plans",
                newName: "Plans");

            migrationBuilder.RenameTable(
                name: "usage_events",
                newName: "UsageEvents");

            migrationBuilder.RenameTable(
                name: "usage_aggregates",
                newName: "UsageAggregates");

            migrationBuilder.RenameIndex(
                name: "IX_subscriptions_PlanId",
                table: "Subscriptions",
                newName: "IX_Subscriptions_PlanId");

            migrationBuilder.RenameIndex(
                name: "IX_usage_events_SubscriptionId",
                table: "UsageEvents",
                newName: "IX_UsageEvents_SubscriptionId");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Plans",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<Guid>(
                name: "TenantId",
                table: "contracts",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Subscriptions",
                table: "Subscriptions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Plans",
                table: "Plans",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UsageEvents",
                table: "UsageEvents",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UsageAggregates",
                table: "UsageAggregates",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_user_settings_UserId",
                table: "user_settings",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rentability_scenarios_UserId",
                table: "rentability_scenarios",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_rentability_scenarios_UserId_IsBase",
                table: "rentability_scenarios",
                columns: new[] { "UserId", "IsBase" });

            migrationBuilder.AddForeignKey(
                name: "FK_Subscriptions_Plans_PlanId",
                table: "Subscriptions",
                column: "PlanId",
                principalTable: "Plans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UsageEvents_Subscriptions_SubscriptionId",
                table: "UsageEvents",
                column: "SubscriptionId",
                principalTable: "Subscriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
