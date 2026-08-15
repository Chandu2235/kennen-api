using Kennen.Domain.Entities;

namespace Kennen.Infrastructure.Persistence.Seeding;

internal static class PricingSeedData
{
    public static IReadOnlyList<PricingPlan> Plans() => new List<PricingPlan>
    {
        new()
        {
            Slug = "starter",
            Category = "ai",
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
            Category = "ai",
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
            Category = "ai",
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
        },
        new()
        {
            Slug = "qa-starter",
            Category = "qa-testing",
            Name = "QA Starter",
            Subtitle = "For focused QA engagements and pilot programs",
            Price = "$2,499",
            BillingPeriod = "/month",
            Description = "A practical quality engineering foundation for one product or delivery team.",
            DisplayOrder = 1,
            Features = new List<PricingPlanFeature>
            {
                new() { Text = "Functional and regression testing", DisplayOrder = 1 },
                new() { Text = "API and integration test coverage", DisplayOrder = 2 },
                new() { Text = "Test planning and reporting", DisplayOrder = 3 },
                new() { Text = "Email support (8x5)", DisplayOrder = 4 }
            }
        },
        new()
        {
            Slug = "qa-professional",
            Category = "qa-testing",
            Name = "QA Professional",
            Subtitle = "For growing teams scaling quality across releases",
            Price = "$7,999",
            BillingPeriod = "/month",
            Description = "Continuous quality engineering with automation, performance, and release assurance.",
            IsFeatured = true,
            DisplayOrder = 2,
            Features = new List<PricingPlanFeature>
            {
                new() { Text = "End-to-end automation testing", DisplayOrder = 1 },
                new() { Text = "Performance and security testing", DisplayOrder = 2 },
                new() { Text = "CI/CD quality gates", DisplayOrder = 3 },
                new() { Text = "Dedicated QA lead", DisplayOrder = 4 },
                new() { Text = "Priority support (12x5)", DisplayOrder = 5 }
            }
        },
        new()
        {
            Slug = "qa-enterprise",
            Category = "qa-testing",
            Name = "QA Enterprise",
            Subtitle = "For mission-critical systems and regulated environments",
            Price = "Custom",
            Description = "A dedicated quality engineering practice aligned to enterprise risk, compliance, and release goals.",
            DisplayOrder = 3,
            Features = new List<PricingPlanFeature>
            {
                new() { Text = "Dedicated QA engineering team", DisplayOrder = 1 },
                new() { Text = "AI-driven testing and analytics", DisplayOrder = 2 },
                new() { Text = "24x7 premium support with SLAs", DisplayOrder = 3 },
                new() { Text = "Compliance and validation assurance", DisplayOrder = 4 },
                new() { Text = "Executive quality governance", DisplayOrder = 5 }
            }
        }
    };
}
