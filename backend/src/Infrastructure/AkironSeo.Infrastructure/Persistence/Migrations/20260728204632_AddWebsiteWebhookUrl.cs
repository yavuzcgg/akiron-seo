using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AkironSeo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWebsiteWebhookUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WebhookUrl",
                table: "Websites",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WebhookUrl",
                table: "Websites");
        }
    }
}
