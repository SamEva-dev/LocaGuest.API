using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocaGuest.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedSaaSPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            
            // Plan FREE
            migrationBuilder.Sql($@"
                INSERT INTO ""plans"" (""Id"", ""Code"", ""Name"", ""Description"", ""MonthlyPrice"", ""AnnualPrice"", ""IsActive"", ""SortOrder"",
                    ""MaxScenarios"", ""MaxExportsPerMonth"", ""MaxVersionsPerScenario"", ""MaxShares"", ""MaxAiSuggestionsPerMonth"", ""MaxWorkspaces"", ""MaxTeamMembers"",
                    ""HasUnlimitedExports"", ""HasUnlimitedVersioning"", ""HasUnlimitedAi"", ""HasPrivateTemplates"", ""HasTeamTemplates"",
                    ""HasAdvancedComparison"", ""HasApiAccess"", ""HasApiReadWrite"", ""HasEmailNotifications"", ""HasSlackIntegration"",
                    ""HasWebhooks"", ""HasSso"", ""HasPrioritySupport"", ""HasDedicatedSupport"", ""CreatedBy"", ""CreatedAt"")
                VALUES (
                    '{Guid.NewGuid()}', 'free', 'Free', 'Plan gratuit pour démarrer', 0, 0, true, 1,
                    3, 5, 3, 1, 2, 1, 1,
                    false, false, false, false, false, false, false, false, false, false, false, false, false, false,
                    'System', timestamp '{now}'
                );
            ");

            // Plan PRO
            migrationBuilder.Sql($@"
                INSERT INTO ""plans"" (""Id"", ""Code"", ""Name"", ""Description"", ""MonthlyPrice"", ""AnnualPrice"", ""IsActive"", ""SortOrder"",
                    ""MaxScenarios"", ""MaxExportsPerMonth"", ""MaxVersionsPerScenario"", ""MaxShares"", ""MaxAiSuggestionsPerMonth"", ""MaxWorkspaces"", ""MaxTeamMembers"",
                    ""HasUnlimitedExports"", ""HasUnlimitedVersioning"", ""HasUnlimitedAi"", ""HasPrivateTemplates"", ""HasTeamTemplates"",
                    ""HasAdvancedComparison"", ""HasApiAccess"", ""HasApiReadWrite"", ""HasEmailNotifications"", ""HasSlackIntegration"",
                    ""HasWebhooks"", ""HasSso"", ""HasPrioritySupport"", ""HasDedicatedSupport"", ""CreatedBy"", ""CreatedAt"")
                VALUES (
                    '{Guid.NewGuid()}', 'pro', 'Pro', 'Pour les professionnels', 29, 290, true, 2,
                    50, 100, 10, 5, 50, 3, 5,
                    false, false, false, true, false, true, true, false, true, false, false, false, true, false,
                    'System', timestamp '{now}'
                );
            ");

            // Plan BUSINESS
            migrationBuilder.Sql($@"
                INSERT INTO ""plans"" (""Id"", ""Code"", ""Name"", ""Description"", ""MonthlyPrice"", ""AnnualPrice"", ""IsActive"", ""SortOrder"",
                    ""MaxScenarios"", ""MaxExportsPerMonth"", ""MaxVersionsPerScenario"", ""MaxShares"", ""MaxAiSuggestionsPerMonth"", ""MaxWorkspaces"", ""MaxTeamMembers"",
                    ""HasUnlimitedExports"", ""HasUnlimitedVersioning"", ""HasUnlimitedAi"", ""HasPrivateTemplates"", ""HasTeamTemplates"",
                    ""HasAdvancedComparison"", ""HasApiAccess"", ""HasApiReadWrite"", ""HasEmailNotifications"", ""HasSlackIntegration"",
                    ""HasWebhooks"", ""HasSso"", ""HasPrioritySupport"", ""HasDedicatedSupport"", ""CreatedBy"", ""CreatedAt"")
                VALUES (
                    '{Guid.NewGuid()}', 'business', 'Business', 'Pour les équipes', 79, 790, true, 3,
                    {int.MaxValue}, 500, 50, 20, 200, 10, 20,
                    true, true, false, true, true, true, true, true, true, true, true, false, true, false,
                    'System', timestamp '{now}'
                );
            ");

            // Plan ENTERPRISE
            migrationBuilder.Sql($@"
                INSERT INTO ""plans"" (""Id"", ""Code"", ""Name"", ""Description"", ""MonthlyPrice"", ""AnnualPrice"", ""IsActive"", ""SortOrder"",
                    ""MaxScenarios"", ""MaxExportsPerMonth"", ""MaxVersionsPerScenario"", ""MaxShares"", ""MaxAiSuggestionsPerMonth"", ""MaxWorkspaces"", ""MaxTeamMembers"",
                    ""HasUnlimitedExports"", ""HasUnlimitedVersioning"", ""HasUnlimitedAi"", ""HasPrivateTemplates"", ""HasTeamTemplates"",
                    ""HasAdvancedComparison"", ""HasApiAccess"", ""HasApiReadWrite"", ""HasEmailNotifications"", ""HasSlackIntegration"",
                    ""HasWebhooks"", ""HasSso"", ""HasPrioritySupport"", ""HasDedicatedSupport"", ""CreatedBy"", ""CreatedAt"")
                VALUES (
                    '{Guid.NewGuid()}', 'enterprise', 'Enterprise', 'Solution sur mesure', 0, 0, true, 4,
                    {int.MaxValue}, {int.MaxValue}, {int.MaxValue}, {int.MaxValue}, {int.MaxValue}, {int.MaxValue}, {int.MaxValue},
                    true, true, true, true, true, true, true, true, true, true, true, true, true, true,
                    'System', timestamp '{now}'
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM ""plans"" WHERE ""Code"" IN ('free', 'pro', 'business', 'enterprise');");
        }
    }
}
