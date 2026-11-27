using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FloristHub.DAL.Migrations
{
    /// <inheritdoc />
    public partial class FloristOnboarding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                table: "Tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "OnboardingColorsAcknowledged",
                table: "Tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OnboardingWizardStep",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PaymentsDisabledByChoice",
                table: "Tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PlatformTermsAcceptedAt",
                table: "Tenants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FloristInvites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FloristInvites", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FloristInvites_TenantId",
                table: "FloristInvites",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_FloristInvites_UserId",
                table: "FloristInvites",
                column: "UserId");

            migrationBuilder.Sql("""
                UPDATE "Tenants"
                SET "IsPublished" = TRUE,
                    "OnboardingColorsAcknowledged" = TRUE,
                    "PlatformTermsAcceptedAt" = COALESCE("PlatformTermsAcceptedAt", NOW()),
                    "PaymentsDisabledByChoice" = TRUE
                WHERE "IsActive" = TRUE;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FloristInvites");

            migrationBuilder.DropColumn(
                name: "IsPublished",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "OnboardingColorsAcknowledged",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "OnboardingWizardStep",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "PaymentsDisabledByChoice",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "PlatformTermsAcceptedAt",
                table: "Tenants");
        }
    }
}
