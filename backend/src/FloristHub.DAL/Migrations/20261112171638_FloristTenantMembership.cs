using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FloristHub.DAL.Migrations
{
    /// <inheritdoc />
    public partial class FloristTenantMembership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FloristTenantMemberships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FloristTenantMemberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FloristTenantMemberships_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FloristTenantMemberships_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FloristTenantMemberships_TenantId",
                table: "FloristTenantMemberships",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_FloristTenantMemberships_UserId_TenantId",
                table: "FloristTenantMemberships",
                columns: new[] { "UserId", "TenantId" },
                unique: true);

            migrationBuilder.Sql("""
                INSERT INTO "FloristTenantMemberships" ("Id", "UserId", "TenantId", "CreatedAt")
                SELECT gen_random_uuid(), u."Id", u."TenantId", NOW()
                FROM "AspNetUsers" u
                INNER JOIN "AspNetUserRoles" ur ON ur."UserId" = u."Id"
                INNER JOIN "AspNetRoles" r ON r."Id" = ur."RoleId" AND r."Name" = 'Florista'
                WHERE u."TenantId" IS NOT NULL
                  AND NOT EXISTS (
                    SELECT 1 FROM "FloristTenantMemberships" m
                    WHERE m."UserId" = u."Id" AND m."TenantId" = u."TenantId");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FloristTenantMemberships");
        }
    }
}
