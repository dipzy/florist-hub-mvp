using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FloristHub.DAL.Migrations
{
    /// <inheritdoc />
    public partial class OrderClientSubmissionId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientSubmissionId",
                table: "Orders",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_TenantId_ClientSubmissionId",
                table: "Orders",
                columns: new[] { "TenantId", "ClientSubmissionId" },
                unique: true,
                filter: "\"ClientSubmissionId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_TenantId_ClientSubmissionId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ClientSubmissionId",
                table: "Orders");
        }
    }
}
