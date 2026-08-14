using Kennen.Domain.Entities;

namespace Kennen.Infrastructure.Persistence.Seeding;

internal static class PricingSeedData
{
    public static IReadOnlyList<PricingPlan> Plans() => new List<PricingPlan>
    {
        Plan("Starter", "For teams beginning their AI journey", "$2,499", "/month",
            new[]
            {
                "AI readiness assessment",
                "Strategy roadmap",
                "1 pilot use case",
                "Email support"
            },
            order: 1),
        Plan("Growth", "For enterprises scaling AI adoption", "$7,499", "/month",
            new[]
            {
                "Everything in Starter",
                "Up to 5 AI use cases",
                "Dedicated AI engineer",
                "RAG pipeline setup",
                "Priority support"
            },
            order: 2,
            isPopular: true),
        Plan("Enterprise", "For orgs transforming at scale", "Custom", "/annum",
            new[]
            {
                "Unlimited AI use cases",
                "Multi-agent orchestration",
                "Custom model development",
                "24×7 global support",
                "Executive advisory"
            },
            order: 3)
    };

    private static PricingPlan Plan(
        string name,
        string subtitle,
        string price,
        string period,
        string[] features,
        int order,
        bool isPopular = false)
    {
        var plan = new PricingPlan
        {
            Name = name,
            Subtitle = subtitle,
            Price = price,
            Period = period,
            DisplayOrder = order,
            IsPopular = isPopular
        };

        for (var i = 0; i < features.Length; i++)
        {
            plan.Features.Add(new PricingPlanFeature
            {
                Text = features[i],
                DisplayOrder = i + 1
            });
        }

        return plan;
    }
}
