using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSaleFulfillmentTimestamps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveredAt",
                table: "Sales",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PreparingSince",
                table: "Sales",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ShippedAt",
                table: "Sales",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "Sales"
                SET "PreparingSince" = "CreatedAt"
                WHERE "FulfillmentStatus" IN (0, 1, 2);

                UPDATE "Sales"
                SET "ShippedAt" = "CreatedAt"
                WHERE "FulfillmentStatus" IN (1, 2);

                UPDATE "Sales"
                SET "DeliveredAt" = "CreatedAt"
                WHERE "FulfillmentStatus" = 2;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveredAt",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "PreparingSince",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "ShippedAt",
                table: "Sales");
        }
    }
}
