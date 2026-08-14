using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kennen.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAiPricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Period",
                table: "PricingPlans",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "IsPopular",
                table: "PricingPlans",
                newName: "IsFeatured");

            migrationBuilder.AddColumn<string>(
                name: "BillingPeriod",
                table: "PricingPlans",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "PricingPlans",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Icon",
                table: "PricingPlanFeatures",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                table: "PricingPlanFeatures",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BillingPeriod",
                table: "PricingPlans");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "PricingPlans");

            migrationBuilder.DropColumn(
                name: "Icon",
                table: "PricingPlanFeatures");

            migrationBuilder.DropColumn(
                name: "IsPublished",
                table: "PricingPlanFeatures");

            migrationBuilder.RenameColumn(
                name: "IsFeatured",
                table: "PricingPlans",
                newName: "IsPopular");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "PricingPlans",
                newName: "Period");
        }
    }
}
