using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Servit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnforceOneResponsePerProvider : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProviderResponses_ServiceRequestId",
                table: "ProviderResponses");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderResponses_ServiceRequestId_ProviderId",
                table: "ProviderResponses",
                columns: new[] { "ServiceRequestId", "ProviderId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProviderResponses_ServiceRequestId_ProviderId",
                table: "ProviderResponses");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderResponses_ServiceRequestId",
                table: "ProviderResponses",
                column: "ServiceRequestId");
        }
    }
}
