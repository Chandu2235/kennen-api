using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kennen.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPhoneAndEngagementToLead : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Engagement",
                table: "leads",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "leads",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Engagement",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "leads");
        }
    }
}
