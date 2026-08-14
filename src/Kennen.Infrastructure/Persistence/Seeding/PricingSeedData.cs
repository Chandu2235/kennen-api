using Kennen.Domain.Entities;

namespace Kennen.Infrastructure.Persistence.Seeding;

internal static class PricingSeedData
{
    public static IReadOnlyList<PricingPlan> Plans() => new List<PricingPlan>
    {
        new()
        {
            Slug = "starter",
            Name = "Starter",
            Subtitle = "For small teams exploring AI",
            Price = "$999",
            BillingPeriod = "/month",
            Description = "Kick-start your AI journey with essential capabilities and guided onboarding.",
            DisplayOrder = 1,
            Features = new List<PricingPlanFeature>
            {
                new() { Text = "1 AI use case implementation", DisplayOrder = 1 },
                new() { Text = "5 model inference endpoints", DisplayOrder = 2 },
                new() { Text = "Email support", DisplayOrder = 3 },
                new() { Text = "Basic analytics dashboard", DisplayOrder = 4 }
            }
        },
        new()
        {
            Slug = "growth",
            Name = "Growth",
            Subtitle = "For scaling AI across the enterprise",
            Price = "$4,999",
            BillingPeriod = "/month",
            Description = "Scale AI pilots into production with governance, integrations, and priority support.",
            IsFeatured = true,
            DisplayOrder = 2,
            Features = new List<PricingPlanFeature>
            {
                new() { Text = "Unlimited AI use cases", DisplayOrder = 1 },
                new() { Text = "RAG and document intelligence", DisplayOrder = 2 },
                new() { Text = "Agentic AI workflow orchestration", DisplayOrder = 3 },
                new() { Text = "Multi-cloud deployment", DisplayOrder = 4 },
                new() { Text = "Dedicated account manager", DisplayOrder = 5 }
            }
        },
        new()
        {
            Slug = "enterprise",
            Name = "Enterprise",
            Subtitle = "Custom AI transformation programs",
            Price = "Custom",
            BillingPeriod = null,
            Description = "Tailored AI strategy, co-innovation, and enterprise-grade SLAs for global organisations.",
            DisplayOrder = 3,
            Features = new List<PricingPlanFeature>
            {
                new() { Text = "Custom model development and fine-tuning", DisplayOrder = 1 },
                new() { Text = "Private cloud or on-premise options", DisplayOrder = 2 },
                new() { Text = "24x7 global support with SLAs", DisplayOrder = 3 },
                new() { Text = "AI governance and risk framework", DisplayOrder = 4 },
                new() { Text = "Executive advisory and training", DisplayOrder = 5 }
            }
        }
    };
}
