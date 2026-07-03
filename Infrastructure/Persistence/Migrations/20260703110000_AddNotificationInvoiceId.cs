using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationInvoiceId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "InvoiceId",
                table: "Notifications",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_InvoiceId",
                table: "Notifications",
                column: "InvoiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Invoices_InvoiceId",
                table: "Notifications",
                column: "InvoiceId",
                principalTable: "Invoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.Sql("""
                UPDATE "Notifications" n
                SET "InvoiceId" = i."Id"
                FROM "Invoices" i
                WHERE n."SaleId" = i."SaleId"
                  AND n."InvoiceId" IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Invoices_InvoiceId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_InvoiceId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "InvoiceId",
                table: "Notifications");
        }
    }
}
