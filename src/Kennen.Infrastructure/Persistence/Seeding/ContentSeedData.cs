using Kennen.Domain.Entities;

namespace Kennen.Infrastructure.Persistence.Seeding;

/// <summary>
/// The copy currently hard-coded in the marketing site's index.html, expressed as data so
/// the CMS starts life matching production exactly. Editing the site should go through the
/// API from here on; this class is only the initial import.
/// </summary>
internal static class ContentSeedData
{
    public static IReadOnlyList<ContentSection> Sections() => new List<ContentSection>
    {
        Section("services", "Services", "Our service portfolio",
            "A comprehensive suite of enterprise technology services engineered to accelerate digital transformation, optimise operations, and future-proof your technology investments.",
            order: 1,
            items: new[]
            {
                ("Software Development", "Custom enterprise application development using modern architectures - microservices, event-driven, and cloud-native - built for scale and resilience.", (string?)"01"),
                ("Quality Engineering", "End-to-end QA with automation testing, API testing, performance testing, and continuous quality integration across your SDLC pipeline.", "02"),
                ("AI Solutions", "Enterprise-grade AI from strategy to deployment - LLMs, generative AI, agentic AI, RAG pipelines, and conversational intelligence platforms.", "03"),
                ("Cloud & DevOps", "Multi-cloud architecture, cloud migration, Kubernetes orchestration, CI/CD pipeline engineering, and infrastructure-as-code at enterprise scale.", "04"),
                ("Data & Business Intelligence", "Data engineering, modern data platforms, real-time analytics, Power BI dashboards, and AI-powered business intelligence for enterprise leaders.", "05"),
                ("ERP & Enterprise Integration", "SAP, Oracle, and Microsoft Dynamics consulting, CRM implementation, and seamless enterprise application integration across your technology ecosystem.", "06"),
                ("Cyber Security", "Zero-trust architecture, vulnerability assessment, penetration testing, and enterprise-grade security operations centre (SOC) services.", "07"),
                ("Digital Marketing", "Performance marketing, SEO/SEM, marketing automation, and data-driven brand growth strategies for enterprise visibility.", "08"),
                ("Workforce Solutions", "Specialised technology recruitment, staff augmentation, and managed workforce delivery across engineering and consulting disciplines.", "09"),
                ("Business Consulting", "Digital strategy advisory, operating model transformation, change management, and enterprise roadmap design for board-level outcomes.", "10")
            }),

        Section("industries", "Industries", "Built for every enterprise vertical",
            "Kennen Technologies brings deep domain expertise across the world's most complex and regulated industries, delivering tailored solutions that address unique sector challenges at scale.",
            order: 2,
            items: new[]
            {
                ("Banking & Financial Services", "Core banking modernisation, fraud detection AI, regulatory compliance automation, and omnichannel digital banking platforms.", (string?)"\U0001F3E6"),
                ("Manufacturing & Industry 4.0", "Smart factory solutions, predictive maintenance, IoT integration, and real-time production analytics for modern manufacturers.", "\U0001F3ED"),
                ("Healthcare & Life Sciences", "Patient data platforms, clinical workflow automation, AI diagnostics support, and HIPAA-compliant cloud infrastructure.", "\U0001F3E5"),
                ("Retail & E-Commerce", "Personalisation engines, inventory intelligence, omnichannel commerce platforms, and AI-powered customer journey optimisation.", "\U0001F6D2"),
                ("Supply Chain & Logistics", "End-to-end supply chain visibility, demand forecasting, warehouse automation, and last-mile delivery optimisation.", "\U0001F69A"),
                ("Telecom & Technology", "Network operations automation, OSS/BSS modernisation, customer experience platforms, and 5G-ready cloud architectures.", "\U0001F4E1"),
                ("Automotive", "Connected vehicle platforms, EV software, and dealer management systems.", "\U0001F697"),
                ("Hospitality", "Property management systems, guest experience AI, and revenue optimisation.", "\U0001F3E8"),
                ("Education", "EdTech platforms, LMS, adaptive learning AI, and campus digital transformation.", "\U0001F393"),
                ("Government", "Digital public services, e-governance platforms, and citizen data systems.", "\U0001F3DB")
            }),

        Section("ai-capabilities", "AI practice", "Enterprise AI - built to scale",
            "Kennen Technologies sits at the frontier of enterprise AI adoption. Our dedicated AI practice combines research-grade capabilities with production-ready engineering to deliver AI systems that transform how enterprises operate, decide, and compete.",
            order: 3,
            items: new[]
            {
                ("Large Language Models (LLMs)", "Fine-tuning, deployment, and governance of LLMs including GPT-4, Claude, Gemini, and open-source models for enterprise use cases.", (string?)null),
                ("RAG & Document Intelligence", "Retrieval-augmented generation pipelines and intelligent document processing that make your enterprise knowledge instantly accessible.", null),
                ("Agentic AI & Automation", "Multi-agent orchestration using CrewAI, AutoGen, and LangChain to automate complex enterprise workflows end-to-end.", null),
                ("Predictive Analytics", "ML-powered forecasting models that drive proactive decision-making across operations, finance, supply chain, and customer management.", null)
            }),

        Section("ai-framework", "Framework", "Our AI adoption framework",
            "A structured path that reduces time-to-value and enterprise risk, taking clients from strategy to scaled production deployment with confidence and measurable ROI.",
            order: 4,
            items: new[]
            {
                ("AI Strategy", "Assess readiness and define the roadmap.", (string?)"1"),
                ("Model Development", "Build, fine-tune, and integrate models.", "2"),
                ("Production Deployment", "Operationalise models at enterprise scale.", "3"),
                ("Monitoring & Optimisation", "Continuously monitor, govern, and improve.", "4")
            }),

        Section("why-us", "Why us", "The Kennen advantage",
            "Enterprises choose Kennen Technologies not just for our technical capabilities - but for our commitment to partnership, accountability, and delivering outcomes that matter to your business.",
            order: 5,
            items: new[]
            {
                ("Deep Enterprise Expertise", "Serving large enterprises across 12 industry verticals with complex, mission-critical technology deployments that demand zero-tolerance for failure.", (string?)null),
                ("Innovation-First Mindset", "We invest continuously in emerging technologies - AI, quantum-ready architectures, and edge computing - so your enterprise always stays ahead of the technology curve.", null),
                ("Agile Delivery at Scale", "Our scaled agile delivery model combines speed with governance - enabling rapid iteration without compromising enterprise-grade security, compliance, or quality standards.", null),
                ("24x7 Global Support", "Round-the-clock support across time zones with dedicated account managers, escalation SLAs, and proactive monitoring to keep your systems performing at peak levels.", null),
                ("Certified Professionals", "Certified engineers holding credentials from AWS, Microsoft, Google Cloud, SAP, PMP, and leading AI platforms - ensuring best-practice delivery on every engagement.", null),
                ("Global Delivery Model", "Onshore, nearshore, and offshore delivery centres operating in a unified model that optimises cost, quality, and responsiveness for enterprise engagements of any scale.", null)
            }),

        Section("qa-testing-hero", "QA & Testing", "Enterprise quality engineering for mission-critical systems",
            "Kennen Technologies delivers end-to-end quality assurance and testing services that reduce risk, accelerate release cycles, and ensure your enterprise applications perform flawlessly across complex, regulated environments.",
            order: 6,
            items: Array.Empty<(string Title, string Summary, string? Icon)>()),

        Section("qa-testing", "Industry Domain Experience", "Deep domain assurance across every vertical",
            "Our QA and testing practice brings real-world experience across the most regulated and transaction-heavy industries. We understand the compliance, security, and performance standards your sector demands.",
            order: 7,
            items: new[]
            {
                ("Payments & Finance", "Deep expertise in payment gateway testing, financial transactions, and compliance validation across Mastercard and Global Payments projects.", (string?)null),
                ("E-Commerce", "Comprehensive testing of online retail platforms, checkout flows, inventory management, and customer experience optimization.", null),
                ("Supply Chain", "Quality assurance for logistics systems, warehouse management, and distribution tracking applications.", null),
                ("Insurance", "Testing insurance policy management systems, claims processing, and regulatory compliance features.", null),
                ("Life Sciences", "Validation of healthcare applications, ensuring data integrity and regulatory compliance in medical software.", null),
                ("Automotive", "Testing automotive software solutions, connected vehicle platforms, and dealership management systems.", null),
                ("Hospitality", "Quality assurance for hotel management systems, booking platforms, and guest experience applications.", null),
                ("Banking & ERP", "Testing enterprise resource planning systems and core banking applications with focus on data security.", null)
            })
    };

    public static IReadOnlyList<StatMetric> Stats() => new List<StatMetric>
    {
        new() { Value = "40%", Label = "Cost Reduction", Description = "Average operational cost savings reported by enterprise clients post-transformation.", DisplayOrder = 1 },
        new() { Value = "62%", Label = "Downtime Reduction", Description = "Average improvement in system uptime through predictive AI and DevOps practices.", DisplayOrder = 2 },
        new() { Value = "98%", Label = "On-Time Delivery", Description = "Project delivery success rate across all enterprise engagements.", DisplayOrder = 3 }
    };

    public static IReadOnlyList<Testimonial> Testimonials() => new List<Testimonial>
    {
        new()
        {
            Quote = "Kennen Technologies transformed our core banking platform in 1 months - a project we thought would take five years. Their AI-powered automation reduced our operational costs by 40% and dramatically improved our customer NPS scores.",
            AuthorInitials = "CT",
            AuthorTitle = "Chief Technology Officer",
            Organisation = "Leading Private Sector Bank",
            DisplayOrder = 1
        },
        new()
        {
            Quote = "The predictive maintenance solution Kennen deployed on our manufacturing floor reduced unplanned downtime by 62%. Their team's domain knowledge in Industry 4.0 was exceptional - they understood our challenges before we even fully articulated them.",
            AuthorInitials = "VP",
            AuthorTitle = "VP of Operations",
            Organisation = "Global Automotive Manufacturer",
            DisplayOrder = 2
        },
        new()
        {
            Quote = "From cloud migration to AI adoption strategy, Kennen has been an end-to-end partner for our digital transformation journey. Their quality engineering practice ensured zero-defect releases across 12 consecutive production deployments.",
            AuthorInitials = "HD",
            AuthorTitle = "Head of Digital",
            Organisation = "Enterprise Retail Group",
            DisplayOrder = 3
        }
    };

    private static ContentSection Section(
        string key,
        string eyebrow,
        string heading,
        string description,
        int order,
        (string Title, string Summary, string? Icon)[] items)
    {
        var section = new ContentSection
        {
            Key = key,
            Eyebrow = eyebrow,
            Heading = heading,
            Description = description,
            DisplayOrder = order
        };

        for (var i = 0; i < items.Length; i++)
        {
            section.Items.Add(new ContentItem
            {
                Title = items[i].Title,
                Summary = items[i].Summary,
                Icon = items[i].Icon,
                DisplayOrder = i + 1
            });
        }

        return section;
    }
}
