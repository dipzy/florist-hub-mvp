using System;
using FloristHub.DAL.Legal;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FloristHub.DAL.Migrations
{
    /// <inheritdoc />
    public partial class GdprLegalCompliance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AcceptedPrivacyVersion",
                table: "Orders",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AcceptedTermsVersion",
                table: "Orders",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LegalConsentRecordedAt",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AcceptedPrivacyAt",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AcceptedPrivacyVersion",
                table: "AspNetUsers",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AcceptedTermsAt",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AcceptedTermsVersion",
                table: "AspNetUsers",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AnonymizedAt",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAnonymized",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MarketingConsent",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "MarketingConsentAt",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LegalDocumentVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentType = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ContentMarkdown = table.Column<string>(type: "character varying(64000)", maxLength: 64000, nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsCurrent = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegalDocumentVersions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LegalDocumentVersions_DocumentType_IsCurrent",
                table: "LegalDocumentVersions",
                columns: new[] { "DocumentType", "IsCurrent" });

            migrationBuilder.CreateIndex(
                name: "IX_LegalDocumentVersions_DocumentType_Version",
                table: "LegalDocumentVersions",
                columns: new[] { "DocumentType", "Version" },
                unique: true);

            var published = new DateTimeOffset(2026, 11, 10, 0, 0, 0, TimeSpan.Zero);
            migrationBuilder.InsertData(
                table: "LegalDocumentVersions",
                columns: ["Id", "DocumentType", "Version", "Title", "ContentMarkdown", "PublishedAt", "IsCurrent"],
                values: new object[,]
                {
                    {
                        new Guid("6f3d8c2a-9b1e-4a7c-8d2f-1e5a9b3c7d01"),
                        0,
                        LegalDocumentSeedContent.SeedVersion,
                        "Termos e Condições",
                        LegalDocumentSeedContent.TermsBody(),
                        published,
                        true
                    },
                    {
                        new Guid("6f3d8c2a-9b1e-4a7c-8d2f-1e5a9b3c7d02"),
                        1,
                        LegalDocumentSeedContent.SeedVersion,
                        "Política de Privacidade",
                        LegalDocumentSeedContent.PrivacyBody(),
                        published,
                        true
                    },
                    {
                        new Guid("6f3d8c2a-9b1e-4a7c-8d2f-1e5a9b3c7d03"),
                        2,
                        LegalDocumentSeedContent.SeedVersion,
                        "Política de Cookies",
                        LegalDocumentSeedContent.CookiesBody(),
                        published,
                        true
                    },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LegalDocumentVersions");

            migrationBuilder.DropColumn(
                name: "AcceptedPrivacyVersion",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "AcceptedTermsVersion",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "LegalConsentRecordedAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "AcceptedPrivacyAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "AcceptedPrivacyVersion",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "AcceptedTermsAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "AcceptedTermsVersion",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "AnonymizedAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IsAnonymized",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "MarketingConsent",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "MarketingConsentAt",
                table: "AspNetUsers");
        }
    }
}
