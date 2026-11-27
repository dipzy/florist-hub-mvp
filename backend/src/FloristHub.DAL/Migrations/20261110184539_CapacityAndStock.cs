using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FloristHub.DAL.Migrations
{
    /// <inheritdoc />
    public partial class CapacityAndStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CapacityHeld",
                table: "Orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateOnly>(
                name: "CapacityServiceDate",
                table: "Orders",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CapacityWindowKey",
                table: "Orders",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "StockCommitted",
                table: "Orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<bool>(
                name: "RecurringSubscriptions",
                table: "NotificationPreferences",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AddColumn<bool>(
                name: "LowStock",
                table: "NotificationPreferences",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "MinimumStockQuantity",
                table: "Flowers",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DeliveryCapacityExceptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsClosed = table.Column<bool>(type: "boolean", nullable: false),
                    DailyOrderLimitOverride = table.Column<int>(type: "integer", nullable: true),
                    Note = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryCapacityExceptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryCapacityHolds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ScopeKey = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    BookedCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryCapacityHolds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryCapacityPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    DailyOrderLimit = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryCapacityPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryCapacityWindows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    OrderLimit = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryCapacityWindows", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StockMovements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FlowerId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    Delta = table.Column<int>(type: "integer", nullable: false),
                    BalanceAfter = table.Column<int>(type: "integer", nullable: true),
                    Reason = table.Column<int>(type: "integer", nullable: false),
                    Note = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockMovements_Flowers_FlowerId",
                        column: x => x.FlowerId,
                        principalTable: "Flowers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryCapacityExceptions_TenantId_ServiceDate",
                table: "DeliveryCapacityExceptions",
                columns: new[] { "TenantId", "ServiceDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryCapacityHolds_TenantId_ServiceDate_ScopeKey",
                table: "DeliveryCapacityHolds",
                columns: new[] { "TenantId", "ServiceDate", "ScopeKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryCapacityPolicies_TenantId",
                table: "DeliveryCapacityPolicies",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryCapacityWindows_TenantId_SortOrder",
                table: "DeliveryCapacityWindows",
                columns: new[] { "TenantId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_FlowerId",
                table: "StockMovements",
                column: "FlowerId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_TenantId_FlowerId_CreatedAt",
                table: "StockMovements",
                columns: new[] { "TenantId", "FlowerId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeliveryCapacityExceptions");

            migrationBuilder.DropTable(
                name: "DeliveryCapacityHolds");

            migrationBuilder.DropTable(
                name: "DeliveryCapacityPolicies");

            migrationBuilder.DropTable(
                name: "DeliveryCapacityWindows");

            migrationBuilder.DropTable(
                name: "StockMovements");

            migrationBuilder.DropColumn(
                name: "CapacityHeld",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CapacityServiceDate",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CapacityWindowKey",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "StockCommitted",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "LowStock",
                table: "NotificationPreferences");

            migrationBuilder.DropColumn(
                name: "MinimumStockQuantity",
                table: "Flowers");

            migrationBuilder.AlterColumn<bool>(
                name: "RecurringSubscriptions",
                table: "NotificationPreferences",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);
        }
    }
}
