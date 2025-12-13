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
            migrationBuilder.Sql(@"
                -- FREE Plan
                INSERT INTO plans (""Id"", ""Code"", ""Name"", ""Description"", ""MonthlyPrice"", ""AnnualPrice"", ""IsActive"", ""SortOrder"",
                    ""MaxScenarios"", ""MaxExportsPerMonth"", ""MaxVersionsPerScenario"", ""MaxShares"", ""MaxAiSuggestionsPerMonth"", ""MaxWorkspaces"", ""MaxTeamMembers"",
                    ""HasUnlimitedExports"", ""HasUnlimitedVersioning"", ""HasUnlimitedAi"", ""HasPrivateTemplates"", ""HasTeamTemplates"", 
                    ""HasAdvancedComparison"", ""HasApiAccess"", ""HasApiReadWrite"", ""HasEmailNotifications"", ""HasSlackIntegration"", 
                    ""HasWebhooks"", ""HasSso"", ""HasPrioritySupport"", ""HasDedicatedSupport"", ""CreatedBy"", ""CreatedAt"")
                VALUES (gen_random_uuid(), 'free', 'Free', 'Plan gratuit pour découvrir LocaGuest', 0, 0, true, 1,
                    3, 10, 5, 2, 10, 1, 1,
                    false, false, false, false, false, false, false, false, true, false, false, false, false, false,
                    'System', NOW());
                
                -- PRO Plan
                INSERT INTO plans (""Id"", ""Code"", ""Name"", ""Description"", ""MonthlyPrice"", ""AnnualPrice"", ""IsActive"", ""SortOrder"",
                    ""MaxScenarios"", ""MaxExportsPerMonth"", ""MaxVersionsPerScenario"", ""MaxShares"", ""MaxAiSuggestionsPerMonth"", ""MaxWorkspaces"", ""MaxTeamMembers"",
                    ""HasUnlimitedExports"", ""HasUnlimitedVersioning"", ""HasUnlimitedAi"", ""HasPrivateTemplates"", ""HasTeamTemplates"", 
                    ""HasAdvancedComparison"", ""HasApiAccess"", ""HasApiReadWrite"", ""HasEmailNotifications"", ""HasSlackIntegration"", 
                    ""HasWebhooks"", ""HasSso"", ""HasPrioritySupport"", ""HasDedicatedSupport"", ""CreatedBy"", ""CreatedAt"")
                VALUES (gen_random_uuid(), 'pro', 'Pro', 'Plan professionnel pour les particuliers et petites agences', 29, 290, true, 2,
                    25, 100, 20, 10, 100, 3, 5,
                    false, true, false, true, false, true, false, false, true, false, false, false, true, false,
                    'System', NOW());
                
                -- BUSINESS Plan
                INSERT INTO plans (""Id"", ""Code"", ""Name"", ""Description"", ""MonthlyPrice"", ""AnnualPrice"", ""IsActive"", ""SortOrder"",
                    ""MaxScenarios"", ""MaxExportsPerMonth"", ""MaxVersionsPerScenario"", ""MaxShares"", ""MaxAiSuggestionsPerMonth"", ""MaxWorkspaces"", ""MaxTeamMembers"",
                    ""HasUnlimitedExports"", ""HasUnlimitedVersioning"", ""HasUnlimitedAi"", ""HasPrivateTemplates"", ""HasTeamTemplates"", 
                    ""HasAdvancedComparison"", ""HasApiAccess"", ""HasApiReadWrite"", ""HasEmailNotifications"", ""HasSlackIntegration"", 
                    ""HasWebhooks"", ""HasSso"", ""HasPrioritySupport"", ""HasDedicatedSupport"", ""CreatedBy"", ""CreatedAt"")
                VALUES (gen_random_uuid(), 'business', 'Business', 'Plan business pour les agences immobilières', 79, 790, true, 3,
                    100, 500, 50, 50, 500, 10, 20,
                    true, true, false, true, true, true, true, false, true, true, true, false, true, false,
                    'System', NOW());
                
                -- ENTERPRISE Plan
                INSERT INTO plans (""Id"", ""Code"", ""Name"", ""Description"", ""MonthlyPrice"", ""AnnualPrice"", ""IsActive"", ""SortOrder"",
                    ""MaxScenarios"", ""MaxExportsPerMonth"", ""MaxVersionsPerScenario"", ""MaxShares"", ""MaxAiSuggestionsPerMonth"", ""MaxWorkspaces"", ""MaxTeamMembers"",
                    ""HasUnlimitedExports"", ""HasUnlimitedVersioning"", ""HasUnlimitedAi"", ""HasPrivateTemplates"", ""HasTeamTemplates"", 
                    ""HasAdvancedComparison"", ""HasApiAccess"", ""HasApiReadWrite"", ""HasEmailNotifications"", ""HasSlackIntegration"", 
                    ""HasWebhooks"", ""HasSso"", ""HasPrioritySupport"", ""HasDedicatedSupport"", ""CreatedBy"", ""CreatedAt"")
                VALUES (gen_random_uuid(), 'enterprise', 'Enterprise', 'Plan entreprise sur mesure', 0, 0, true, 4,
                    999999, 999999, 999999, 999999, 999999, 999999, 999999,
                    true, true, true, true, true, true, true, true, true, true, true, true, true, true,
                    'System', NOW());
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
