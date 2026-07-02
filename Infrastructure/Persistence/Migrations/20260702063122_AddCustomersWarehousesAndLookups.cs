using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomersWarehousesAndLookups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryMovements_Products_ProductId",
                table: "InventoryMovements");

            migrationBuilder.AddColumn<Guid>(
                name: "CustomerId",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ChatSessionId",
                table: "Sales",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "Sales",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CustomerId",
                table: "Sales",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CustomerId",
                table: "Invoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProductId",
                table: "InvoiceLineItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InventoryId",
                table: "InventoryMovements",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DocumentNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MovementTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovementTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductEmbeddings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentText = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Embedding = table.Column<Vector>(type: "vector(1536)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductEmbeddings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductEmbeddings_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SaleOrigins",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaleOrigins", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SaleStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaleStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Warehouses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warehouses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChatSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionToken = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CurrentStateJson = table.Column<string>(type: "jsonb", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatSessions_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ChatSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Inventories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentStock = table.Column<int>(type: "integer", nullable: false),
                    MinStock = table.Column<int>(type: "integer", nullable: false),
                    MaxStock = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inventories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Inventories_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Inventories_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChatMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChatSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderType = table.Column<int>(type: "integer", nullable: false),
                    MessageText = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    MetadataJson = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatMessages_ChatSessions_ChatSessionId",
                        column: x => x.ChatSessionId,
                        principalTable: "ChatSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_CustomerId",
                table: "Users",
                column: "CustomerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sales_ChatSessionId",
                table: "Sales",
                column: "ChatSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_CreatedByUserId",
                table: "Sales",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_CustomerId",
                table: "Sales",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_CustomerId",
                table: "Invoices",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLineItems_ProductId",
                table: "InvoiceLineItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_InventoryId",
                table: "InventoryMovements",
                column: "InventoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ChatSessionId",
                table: "ChatMessages",
                column: "ChatSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_CreatedAt",
                table: "ChatMessages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_CustomerId",
                table: "ChatSessions",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_SessionToken",
                table: "ChatSessions",
                column: "SessionToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_UserId",
                table: "ChatSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Email",
                table: "Customers",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_ProductId_WarehouseId",
                table: "Inventories",
                columns: new[] { "ProductId", "WarehouseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_WarehouseId",
                table: "Inventories",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceStatuses_Name",
                table: "InvoiceStatuses",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MovementTypes_Name",
                table: "MovementTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductEmbeddings_ProductId",
                table: "ProductEmbeddings",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleOrigins_Name",
                table: "SaleOrigins",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SaleStatuses_Name",
                table: "SaleStatuses",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_Name",
                table: "Warehouses",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryMovements_Inventories_InventoryId",
                table: "InventoryMovements",
                column: "InventoryId",
                principalTable: "Inventories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryMovements_Products_ProductId",
                table: "InventoryMovements",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceLineItems_Products_ProductId",
                table: "InvoiceLineItems",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Customers_CustomerId",
                table: "Invoices",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_ChatSessions_ChatSessionId",
                table: "Sales",
                column: "ChatSessionId",
                principalTable: "ChatSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_Customers_CustomerId",
                table: "Sales",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_Users_CreatedByUserId",
                table: "Sales",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Customers_CustomerId",
                table: "Users",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            SeedLookupsAndWarehouses(migrationBuilder);
            MigrateCustomers(migrationBuilder);
            MigrateInventory(migrationBuilder);
            MigrateEmbeddings(migrationBuilder);

            migrationBuilder.DropColumn(name: "Embedding", table: "Products");
            migrationBuilder.DropColumn(name: "MaxStock", table: "Products");
            migrationBuilder.DropColumn(name: "Stock", table: "Products");
            migrationBuilder.DropColumn(name: "Warehouse", table: "Products");

            migrationBuilder.Sql(
                """
                CREATE INDEX IF NOT EXISTS "IX_ProductEmbeddings_Embedding_HNSW"
                ON "ProductEmbeddings" USING hnsw ("Embedding" vector_cosine_ops);
                """);
        }

        private static void SeedLookupsAndWarehouses(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO "SaleStatuses" ("Id", "Name") VALUES
                    (0, 'invoiced'), (1, 'pending'), (2, 'confirmed'), (3, 'cancelled')
                ON CONFLICT ("Id") DO NOTHING;

                INSERT INTO "SaleOrigins" ("Id", "Name") VALUES
                    (0, 'manual'), (1, 'chatbot')
                ON CONFLICT ("Id") DO NOTHING;

                INSERT INTO "InvoiceStatuses" ("Id", "Name") VALUES
                    (0, 'paid'), (1, 'pending'), (2, 'overdue'), (3, 'draft')
                ON CONFLICT ("Id") DO NOTHING;

                INSERT INTO "MovementTypes" ("Id", "Name") VALUES
                    (0, 'inbound'), (1, 'adjustment'), (2, 'outbound')
                ON CONFLICT ("Id") DO NOTHING;

                INSERT INTO "Warehouses" ("Id", "Name", "Location", "IsActive", "IsDefault") VALUES
                    ('10000000-0000-0000-0000-000000000001', 'Central Bogotá', 'Bogotá D.C.', TRUE, TRUE),
                    ('10000000-0000-0000-0000-000000000002', 'Almacén Norte', 'Medellín', TRUE, FALSE),
                    ('10000000-0000-0000-0000-000000000003', 'Bodega Sur', 'Cali', TRUE, FALSE)
                ON CONFLICT ("Id") DO NOTHING;
                """);
        }

        private static void MigrateCustomers(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO "Customers" ("Id", "FullName", "Email", "CreatedAt")
                SELECT gen_random_uuid(), src."FullName", src."Email", NOW()
                FROM (
                    SELECT DISTINCT TRIM("CustomerName") AS "FullName", LOWER(TRIM("CustomerEmail")) AS "Email"
                    FROM "Sales"
                    WHERE TRIM("CustomerEmail") <> ''
                    UNION
                    SELECT DISTINCT TRIM("ClientName"), LOWER(TRIM("ClientName")) || '@migrated.local'
                    FROM "Invoices"
                    WHERE TRIM("ClientName") <> ''
                ) src
                WHERE NOT EXISTS (
                    SELECT 1 FROM "Customers" c WHERE LOWER(c."Email") = src."Email"
                );

                UPDATE "Sales" s
                SET "CustomerId" = c."Id"
                FROM "Customers" c
                WHERE LOWER(c."Email") = LOWER(TRIM(s."CustomerEmail"));

                UPDATE "Invoices" i
                SET "CustomerId" = c."Id"
                FROM "Customers" c
                WHERE c."FullName" = TRIM(i."ClientName")
                  AND i."CustomerId" IS NULL;
                """);
        }

        private static void MigrateInventory(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO "Inventories" ("Id", "ProductId", "WarehouseId", "CurrentStock", "MinStock", "MaxStock", "UpdatedAt")
                SELECT
                    gen_random_uuid(),
                    p."Id",
                    COALESCE(w."Id", '10000000-0000-0000-0000-000000000001'),
                    p."Stock",
                    GREATEST(1, p."MaxStock" / 4),
                    p."MaxStock",
                    NOW()
                FROM "Products" p
                LEFT JOIN "Warehouses" w ON w."Name" = p."Warehouse"
                ON CONFLICT ("ProductId", "WarehouseId") DO NOTHING;

                UPDATE "InventoryMovements" m
                SET "InventoryId" = i."Id"
                FROM "Inventories" i
                WHERE i."ProductId" = m."ProductId"
                  AND m."InventoryId" IS NULL;
                """);
        }

        private static void MigrateEmbeddings(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO "ProductEmbeddings" ("Id", "ProductId", "ContentText", "Embedding", "CreatedAt")
                SELECT gen_random_uuid(), p."Id", COALESCE(p."Description", p."Name"), p."Embedding", NOW()
                FROM "Products" p
                WHERE p."Embedding" IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryMovements_Inventories_InventoryId",
                table: "InventoryMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryMovements_Products_ProductId",
                table: "InventoryMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceLineItems_Products_ProductId",
                table: "InvoiceLineItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Customers_CustomerId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Sales_ChatSessions_ChatSessionId",
                table: "Sales");

            migrationBuilder.DropForeignKey(
                name: "FK_Sales_Customers_CustomerId",
                table: "Sales");

            migrationBuilder.DropForeignKey(
                name: "FK_Sales_Users_CreatedByUserId",
                table: "Sales");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Customers_CustomerId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "ChatMessages");

            migrationBuilder.DropTable(
                name: "Inventories");

            migrationBuilder.DropTable(
                name: "InvoiceStatuses");

            migrationBuilder.DropTable(
                name: "MovementTypes");

            migrationBuilder.DropTable(
                name: "ProductEmbeddings");

            migrationBuilder.DropTable(
                name: "SaleOrigins");

            migrationBuilder.DropTable(
                name: "SaleStatuses");

            migrationBuilder.DropTable(
                name: "ChatSessions");

            migrationBuilder.DropTable(
                name: "Warehouses");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Users_CustomerId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Sales_ChatSessionId",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Sales_CreatedByUserId",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Sales_CustomerId",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_CustomerId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_InvoiceLineItems_ProductId",
                table: "InvoiceLineItems");

            migrationBuilder.DropIndex(
                name: "IX_InventoryMovements_InventoryId",
                table: "InventoryMovements");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ChatSessionId",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "InvoiceLineItems");

            migrationBuilder.DropColumn(
                name: "InventoryId",
                table: "InventoryMovements");

            migrationBuilder.AlterColumn<decimal>(
                name: "UnitPrice",
                table: "SaleLineItems",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<Vector>(
                name: "Embedding",
                table: "Products",
                type: "vector(1536)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxStock",
                table: "Products",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Stock",
                table: "Products",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Warehouse",
                table: "Products",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryMovements_Products_ProductId",
                table: "InventoryMovements",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
