using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kennen.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPricingCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "PricingPlans",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "PricingPlans");
        }
    }
}
