using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddSaleMeasureUnits : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "SaleUnit",
            table: "Products",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AlterColumn<decimal>(
            name: "CurrentStock",
            table: "Inventories",
            type: "numeric(18,4)",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "integer");

        migrationBuilder.AlterColumn<decimal>(
            name: "MaxStock",
            table: "Inventories",
            type: "numeric(18,4)",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "integer");

        migrationBuilder.AlterColumn<decimal>(
            name: "MinStock",
            table: "Inventories",
            type: "numeric(18,4)",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "integer");

        migrationBuilder.AlterColumn<decimal>(
            name: "QuantityChange",
            table: "InventoryMovements",
            type: "numeric(18,4)",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "integer");

        migrationBuilder.AddColumn<int>(
            name: "MeasureUnit",
            table: "SaleLineItems",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AlterColumn<decimal>(
            name: "Quantity",
            table: "SaleLineItems",
            type: "numeric(18,4)",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "integer");

        migrationBuilder.AddColumn<int>(
            name: "MeasureUnit",
            table: "InvoiceLineItems",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AlterColumn<decimal>(
            name: "Quantity",
            table: "InvoiceLineItems",
            type: "numeric(18,4)",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "integer");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "SaleUnit",
            table: "Products");

        migrationBuilder.AlterColumn<int>(
            name: "CurrentStock",
            table: "Inventories",
            type: "integer",
            nullable: false,
            oldClrType: typeof(decimal),
            oldType: "numeric(18,4)");

        migrationBuilder.AlterColumn<int>(
            name: "MaxStock",
            table: "Inventories",
            type: "integer",
            nullable: false,
            oldClrType: typeof(decimal),
            oldType: "numeric(18,4)");

        migrationBuilder.AlterColumn<int>(
            name: "MinStock",
            table: "Inventories",
            type: "integer",
            nullable: false,
            oldClrType: typeof(decimal),
            oldType: "numeric(18,4)");

        migrationBuilder.AlterColumn<int>(
            name: "QuantityChange",
            table: "InventoryMovements",
            type: "integer",
            nullable: false,
            oldClrType: typeof(decimal),
            oldType: "numeric(18,4)");

        migrationBuilder.DropColumn(
            name: "MeasureUnit",
            table: "SaleLineItems");

        migrationBuilder.AlterColumn<int>(
            name: "Quantity",
            table: "SaleLineItems",
            type: "integer",
            nullable: false,
            oldClrType: typeof(decimal),
            oldType: "numeric(18,4)");

        migrationBuilder.DropColumn(
            name: "MeasureUnit",
            table: "InvoiceLineItems");

        migrationBuilder.AlterColumn<int>(
            name: "Quantity",
            table: "InvoiceLineItems",
            type: "integer",
            nullable: false,
            oldClrType: typeof(decimal),
            oldType: "numeric(18,4)");
    }
}
