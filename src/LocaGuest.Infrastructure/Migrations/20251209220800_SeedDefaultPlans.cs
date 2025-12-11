using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocaGuest.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedDefaultPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            
            // Plan FREE
            migrationBuilder.Sql($@"
                INSERT INTO Plans (Id, Code, Name, Description, MonthlyPrice, AnnualPrice, IsActive, SortOrder,
                    MaxScenarios, MaxExportsPerMonth, MaxVersionsPerScenario, MaxShares, MaxAiSuggestionsPerMonth, MaxWorkspaces, MaxTeamMembers,
                    HasUnlimitedExports, HasUnlimitedVersioning, HasUnlimitedAi, HasPrivateTemplates, HasTeamTemplates,
                    HasAdvancedComparison, HasApiAccess, HasApiReadWrite, HasEmailNotifications, HasSlackIntegration,
                    HasWebhooks, HasSso, HasPrioritySupport, HasDedicatedSupport, CreatedBy, CreatedAt)
                VALUES (
                    '{Guid.NewGuid()}', 'free', 'Free', 'Plan gratuit pour démarrer', 0, 0, 1, 1,
                    3, 5, 3, 1, 2, 1, 1,
                    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                    'System', '{now}'
                );
            ");

            // Plan PRO
            migrationBuilder.Sql($@"
                INSERT INTO Plans (Id, Code, Name, Description, MonthlyPrice, AnnualPrice, IsActive, SortOrder,
                    MaxScenarios, MaxExportsPerMonth, MaxVersionsPerScenario, MaxShares, MaxAiSuggestionsPerMonth, MaxWorkspaces, MaxTeamMembers,
                    HasUnlimitedExports, HasUnlimitedVersioning, HasUnlimitedAi, HasPrivateTemplates, HasTeamTemplates,
                    HasAdvancedComparison, HasApiAccess, HasApiReadWrite, HasEmailNotifications, HasSlackIntegration,
                    HasWebhooks, HasSso, HasPrioritySupport, HasDedicatedSupport, CreatedBy, CreatedAt)
                VALUES (
                    '{Guid.NewGuid()}', 'pro', 'Pro', 'Pour les professionnels', 29, 290, 1, 2,
                    50, 100, 10, 5, 50, 3, 5,
                    0, 0, 0, 1, 0, 1, 1, 0, 1, 0, 0, 0, 1, 0,
                    'System', '{now}'
                );
            ");

            // Plan BUSINESS
            migrationBuilder.Sql($@"
                INSERT INTO Plans (Id, Code, Name, Description, MonthlyPrice, AnnualPrice, IsActive, SortOrder,
                    MaxScenarios, MaxExportsPerMonth, MaxVersionsPerScenario, MaxShares, MaxAiSuggestionsPerMonth, MaxWorkspaces, MaxTeamMembers,
                    HasUnlimitedExports, HasUnlimitedVersioning, HasUnlimitedAi, HasPrivateTemplates, HasTeamTemplates,
                    HasAdvancedComparison, HasApiAccess, HasApiReadWrite, HasEmailNotifications, HasSlackIntegration,
                    HasWebhooks, HasSso, HasPrioritySupport, HasDedicatedSupport, CreatedBy, CreatedAt)
                VALUES (
                    '{Guid.NewGuid()}', 'business', 'Business', 'Pour les équipes', 79, 790, 1, 3,
                    {int.MaxValue}, 500, 50, 20, 200, 10, 20,
                    1, 1, 0, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 0,
                    'System', '{now}'
                );
            ");

            // Plan ENTERPRISE
            migrationBuilder.Sql($@"
                INSERT INTO Plans (Id, Code, Name, Description, MonthlyPrice, AnnualPrice, IsActive, SortOrder,
                    MaxScenarios, MaxExportsPerMonth, MaxVersionsPerScenario, MaxShares, MaxAiSuggestionsPerMonth, MaxWorkspaces, MaxTeamMembers,
                    HasUnlimitedExports, HasUnlimitedVersioning, HasUnlimitedAi, HasPrivateTemplates, HasTeamTemplates,
                    HasAdvancedComparison, HasApiAccess, HasApiReadWrite, HasEmailNotifications, HasSlackIntegration,
                    HasWebhooks, HasSso, HasPrioritySupport, HasDedicatedSupport, CreatedBy, CreatedAt)
                VALUES (
                    '{Guid.NewGuid()}', 'enterprise', 'Enterprise', 'Solution sur mesure', 0, 0, 1, 4,
                    {int.MaxValue}, {int.MaxValue}, {int.MaxValue}, {int.MaxValue}, {int.MaxValue}, {int.MaxValue}, {int.MaxValue},
                    1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
                    'System', '{now}'
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM Plans WHERE Code IN ('free', 'pro', 'business', 'enterprise');");
        }
    }
}
