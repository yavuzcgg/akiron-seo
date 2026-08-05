using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AkironSeo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ScopeGeoAnalysisAndNotificationsToWebsite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // GeoAnalyses.WebsiteId is added as required with a foreign key, so any pre-existing
            // row would be stamped with Guid.Empty and fail that key. These rows are recomputable
            // analysis output, and rows written before this migration also contain the fabricated
            // ChatGPT/Claude engine results that this change removes, so they are purged rather
            // than backfilled. Gold Opportunity alerts raised from that fabricated data are
            // dropped for the same reason; genuine ones will be raised again on the next run.
            migrationBuilder.Sql("""
                DELETE FROM "Notifications" WHERE "Type" = 4; -- NotificationTypeEnum.GoldOpportunityAlert
                DELETE FROM "GeoAnalyses";
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_GeoAnalyses_TrackedKeywords_TrackedKeywordId",
                table: "GeoAnalyses");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_TenantId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_GeoAnalyses_TenantId",
                table: "GeoAnalyses");

            migrationBuilder.AddColumn<Guid>(
                name: "WebsiteId",
                table: "Notifications",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "TrackedKeywordId",
                table: "GeoAnalyses",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "WebsiteId",
                table: "GeoAnalyses",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_TenantId_WebsiteId_Type_CreatedAt",
                table: "Notifications",
                columns: new[] { "TenantId", "WebsiteId", "Type", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_GeoAnalyses_TenantId_WebsiteId_CreatedAt",
                table: "GeoAnalyses",
                columns: new[] { "TenantId", "WebsiteId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_GeoAnalyses_WebsiteId",
                table: "GeoAnalyses",
                column: "WebsiteId");

            migrationBuilder.AddForeignKey(
                name: "FK_GeoAnalyses_TrackedKeywords_TrackedKeywordId",
                table: "GeoAnalyses",
                column: "TrackedKeywordId",
                principalTable: "TrackedKeywords",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_GeoAnalyses_Websites_WebsiteId",
                table: "GeoAnalyses",
                column: "WebsiteId",
                principalTable: "Websites",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GeoAnalyses_TrackedKeywords_TrackedKeywordId",
                table: "GeoAnalyses");

            migrationBuilder.DropForeignKey(
                name: "FK_GeoAnalyses_Websites_WebsiteId",
                table: "GeoAnalyses");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_TenantId_WebsiteId_Type_CreatedAt",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_GeoAnalyses_TenantId_WebsiteId_CreatedAt",
                table: "GeoAnalyses");

            migrationBuilder.DropIndex(
                name: "IX_GeoAnalyses_WebsiteId",
                table: "GeoAnalyses");

            migrationBuilder.DropColumn(
                name: "WebsiteId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "WebsiteId",
                table: "GeoAnalyses");

            migrationBuilder.AlterColumn<Guid>(
                name: "TrackedKeywordId",
                table: "GeoAnalyses",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_TenantId",
                table: "Notifications",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_GeoAnalyses_TenantId",
                table: "GeoAnalyses",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_GeoAnalyses_TrackedKeywords_TrackedKeywordId",
                table: "GeoAnalyses",
                column: "TrackedKeywordId",
                principalTable: "TrackedKeywords",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
