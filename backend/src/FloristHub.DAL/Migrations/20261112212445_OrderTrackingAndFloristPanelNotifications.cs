using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FloristHub.DAL.Migrations
{
    /// <inheritdoc />
    public partial class OrderTrackingAndFloristPanelNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TrackingAccessToken",
                table: "Orders",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "OrderReviews",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "FloristPanelNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    ReadAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FloristPanelNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FloristPanelNotifications_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_TrackingAccessToken",
                table: "Orders",
                column: "TrackingAccessToken",
                unique: true,
                filter: "\"TrackingAccessToken\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FloristPanelNotifications_OrderId",
                table: "FloristPanelNotifications",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_FloristPanelNotifications_TenantId_OrderId_Kind",
                table: "FloristPanelNotifications",
                columns: new[] { "TenantId", "OrderId", "Kind" });

            migrationBuilder.CreateIndex(
                name: "IX_FloristPanelNotifications_TenantId_ReadAt_CreatedAt",
                table: "FloristPanelNotifications",
                columns: new[] { "TenantId", "ReadAt", "CreatedAt" });

            migrationBuilder.Sql("""UPDATE "OrderReviews" SET "IsApproved" = TRUE WHERE "IsHidden" = FALSE;""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FloristPanelNotifications");

            migrationBuilder.DropIndex(
                name: "IX_Orders_TrackingAccessToken",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TrackingAccessToken",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "OrderReviews");
        }
    }
}
