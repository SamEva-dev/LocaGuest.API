using LocaGuest.Domain.Aggregates.SubscriptionAggregate;
using Microsoft.EntityFrameworkCore;

namespace LocaGuest.Infrastructure.Persistence.Seeders;

public static class PlanSeeder
{
    public static async Task SeedPlansAsync(LocaGuestDbContext context)
    {
        if (await context.Plans.AnyAsync())
        {
            return; // Plans déjà créés
        }

        var plans = new[]
        {
            CreateFreePlan(),
            CreateProPlan(),
            CreateBusinessPlan(),
            CreateEnterprisePlan()
        };

        context.Plans.AddRange(plans);
        await context.SaveChangesAsync();
    }

    private static Plan CreateFreePlan()
    {
        var plan = Plan.Create(
            code: "free",
            name: "Free",
            description: "Pour découvrir et tester le Wizard de Rentabilité",
            monthlyPrice: 0m,
            annualPrice: 0m,
            sortOrder: 1
        );

        plan.UpdateLimits(
            maxScenarios: 3,
            maxExportsPerMonth: 5,
            maxVersionsPerScenario: 3,
            maxShares: 1,
            maxAiSuggestionsPerMonth: 2,
            maxWorkspaces: 1,
            maxTeamMembers: 1
        );

        plan.UpdateFeatures(
            unlimitedExports: false,
            unlimitedVersioning: false,
            unlimitedAi: false,
            privateTemplates: false,
            teamTemplates: false,
            advancedComparison: false,
            apiAccess: false,
            apiReadWrite: false,
            emailNotifications: false,
            slackIntegration: false,
            webhooks: false,
            sso: false,
            prioritySupport: false,
            dedicatedSupport: false
        );

        return plan;
    }

    private static Plan CreateProPlan()
    {
        var plan = Plan.Create(
            code: "pro",
            name: "Pro",
            description: "Pour les investisseurs actifs et professionnels",
            monthlyPrice: 19m,
            annualPrice: 182.40m, // -20%
            sortOrder: 2
        );

        plan.UpdateLimits(
            maxScenarios: 50,
            maxExportsPerMonth: int.MaxValue,
            maxVersionsPerScenario: int.MaxValue,
            maxShares: 5,
            maxAiSuggestionsPerMonth: int.MaxValue,
            maxWorkspaces: 1,
            maxTeamMembers: 1
        );

        plan.UpdateFeatures(
            unlimitedExports: true,
            unlimitedVersioning: true,
            unlimitedAi: true,
            privateTemplates: true,
            teamTemplates: false,
            advancedComparison: true,
            apiAccess: false,
            apiReadWrite: false,
            emailNotifications: true,
            slackIntegration: false,
            webhooks: false,
            sso: false,
            prioritySupport: false,
            dedicatedSupport: false
        );

        return plan;
    }

    private static Plan CreateBusinessPlan()
    {
        var plan = Plan.Create(
            code: "business",
            name: "Business",
            description: "Pour les équipes et agences immobilières",
            monthlyPrice: 49m,
            annualPrice: 470.40m, // -20%
            sortOrder: 3
        );

        plan.UpdateLimits(
            maxScenarios: 200,
            maxExportsPerMonth: int.MaxValue,
            maxVersionsPerScenario: int.MaxValue,
            maxShares: 10,
            maxAiSuggestionsPerMonth: int.MaxValue,
            maxWorkspaces: 3,
            maxTeamMembers: 10
        );

        plan.UpdateFeatures(
            unlimitedExports: true,
            unlimitedVersioning: true,
            unlimitedAi: true,
            privateTemplates: true,
            teamTemplates: true,
            advancedComparison: true,
            apiAccess: true,
            apiReadWrite: false,
            emailNotifications: true,
            slackIntegration: true,
            webhooks: true,
            sso: true,
            prioritySupport: true,
            dedicatedSupport: false
        );

        return plan;
    }

    private static Plan CreateEnterprisePlan()
    {
        var plan = Plan.Create(
            code: "enterprise",
            name: "Enterprise",
            description: "Solution sur-mesure pour les grandes organisations",
            monthlyPrice: 0m, // Sur devis
            annualPrice: 0m,
            sortOrder: 4
        );

        plan.UpdateLimits(
            maxScenarios: int.MaxValue,
            maxExportsPerMonth: int.MaxValue,
            maxVersionsPerScenario: int.MaxValue,
            maxShares: int.MaxValue,
            maxAiSuggestionsPerMonth: int.MaxValue,
            maxWorkspaces: int.MaxValue,
            maxTeamMembers: int.MaxValue
        );

        plan.UpdateFeatures(
            unlimitedExports: true,
            unlimitedVersioning: true,
            unlimitedAi: true,
            privateTemplates: true,
            teamTemplates: true,
            advancedComparison: true,
            apiAccess: true,
            apiReadWrite: true,
            emailNotifications: true,
            slackIntegration: true,
            webhooks: true,
            sso: true,
            prioritySupport: true,
            dedicatedSupport: true
        );

        return plan;
    }
}
