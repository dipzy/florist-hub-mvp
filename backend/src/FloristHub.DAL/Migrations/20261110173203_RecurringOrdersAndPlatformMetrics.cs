using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FloristHub.DAL.Migrations
{
    /// <inheritdoc />
    public partial class RecurringOrdersAndPlatformMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RecurringOccurrenceId",
                table: "Orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RecurringSubscriptionId",
                table: "Orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RecurringSubscriptions",
                table: "NotificationPreferences",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "RecurringSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Frequency = table.Column<int>(type: "integer", nullable: false),
                    AnchorDeliveryAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    NextDeliveryAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeliveryType = table.Column<int>(type: "integer", nullable: false),
                    DeliveryAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DeliveryZoneId = table.Column<Guid>(type: "uuid", nullable: true),
                    BaseType = table.Column<int>(type: "integer", nullable: false),
                    ArrangementShape = table.Column<int>(type: "integer", nullable: false),
                    ArrangementVariationSeed = table.Column<int>(type: "integer", nullable: false),
                    ArrangementPreviewUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    CustomerNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsFloristChoice = table.Column<bool>(type: "boolean", nullable: false),
                    FloristChoiceBudget = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    FloristResponseNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AcceptedTermsVersion = table.Column<string>(type: "text", nullable: true),
                    AcceptedPrivacyVersion = table.Column<string>(type: "text", nullable: true),
                    LegalConsentRecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    GenerationLeaseUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurringSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecurringSubscriptions_AspNetUsers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecurringSubscriptions_DeliveryZones_DeliveryZoneId",
                        column: x => x.DeliveryZoneId,
                        principalTable: "DeliveryZones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "RecurringOccurrences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecurringSubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduledDeliveryAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    GeneratedOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurringOccurrences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecurringOccurrences_Orders_GeneratedOrderId",
                        column: x => x.GeneratedOrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RecurringOccurrences_RecurringSubscriptions_RecurringSubscr~",
                        column: x => x.RecurringSubscriptionId,
                        principalTable: "RecurringSubscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecurringSubscriptionItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecurringSubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    FlowerId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurringSubscriptionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecurringSubscriptionItems_RecurringSubscriptions_Recurring~",
                        column: x => x.RecurringSubscriptionId,
                        principalTable: "RecurringSubscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_RecurringOccurrenceId",
                table: "Orders",
                column: "RecurringOccurrenceId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_RecurringSubscriptionId",
                table: "Orders",
                column: "RecurringSubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringOccurrences_GeneratedOrderId",
                table: "RecurringOccurrences",
                column: "GeneratedOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringOccurrences_IdempotencyKey",
                table: "RecurringOccurrences",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecurringOccurrences_RecurringSubscriptionId",
                table: "RecurringOccurrences",
                column: "RecurringSubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringSubscriptionItems_RecurringSubscriptionId",
                table: "RecurringSubscriptionItems",
                column: "RecurringSubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringSubscriptions_CustomerId",
                table: "RecurringSubscriptions",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringSubscriptions_DeliveryZoneId",
                table: "RecurringSubscriptions",
                column: "DeliveryZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringSubscriptions_TenantId_Status_NextDeliveryAt",
                table: "RecurringSubscriptions",
                columns: new[] { "TenantId", "Status", "NextDeliveryAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_RecurringOccurrences_RecurringOccurrenceId",
                table: "Orders",
                column: "RecurringOccurrenceId",
                principalTable: "RecurringOccurrences",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_RecurringSubscriptions_RecurringSubscriptionId",
                table: "Orders",
                column: "RecurringSubscriptionId",
                principalTable: "RecurringSubscriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_RecurringOccurrences_RecurringOccurrenceId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_RecurringSubscriptions_RecurringSubscriptionId",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "RecurringOccurrences");

            migrationBuilder.DropTable(
                name: "RecurringSubscriptionItems");

            migrationBuilder.DropTable(
                name: "RecurringSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Orders_RecurringOccurrenceId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_RecurringSubscriptionId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RecurringOccurrenceId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RecurringSubscriptionId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RecurringSubscriptions",
                table: "NotificationPreferences");
        }
    }
}
