using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WholesaleApi.Migrations
{
    /// <inheritdoc />
    public partial class Faz4_ProductCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // CatalogItems tablosu
            migrationBuilder.CreateTable(
                name: "CatalogItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Brand = table.Column<string>(type: "text", nullable: true),
                    Manufacturer = table.Column<string>(type: "text", nullable: true),
                    Unit = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogItems", x => x.Id);
                });

            // CatalogItemBarcodes tablosu
            migrationBuilder.CreateTable(
                name: "CatalogItemBarcodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CatalogItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Barcode = table.Column<string>(type: "text", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogItemBarcodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CatalogItemBarcodes_CatalogItems_CatalogItemId",
                        column: x => x.CatalogItemId,
                        principalTable: "CatalogItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // CatalogItemImages tablosu
            migrationBuilder.CreateTable(
                name: "CatalogItemImages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CatalogItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    FilePath = table.Column<string>(type: "text", nullable: false),
                    IsMain = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogItemImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CatalogItemImages_CatalogItems_CatalogItemId",
                        column: x => x.CatalogItemId,
                        principalTable: "CatalogItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Products tablosuna CatalogItemId ekle
            migrationBuilder.AddColumn<Guid>(
                name: "CatalogItemId",
                table: "Products",
                type: "uuid",
                nullable: true);

            // Index'ler
            migrationBuilder.CreateIndex(
                name: "IX_CatalogItemBarcodes_CatalogItemId",
                table: "CatalogItemBarcodes",
                column: "CatalogItemId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogItemBarcodes_Barcode",
                table: "CatalogItemBarcodes",
                column: "Barcode");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogItemImages_CatalogItemId",
                table: "CatalogItemImages",
                column: "CatalogItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_CatalogItemId",
                table: "Products",
                column: "CatalogItemId");

            // Products → CatalogItems FK
            migrationBuilder.AddForeignKey(
                name: "FK_Products_CatalogItems_CatalogItemId",
                table: "Products",
                column: "CatalogItemId",
                principalTable: "CatalogItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_CatalogItems_CatalogItemId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_CatalogItemId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CatalogItemId",
                table: "Products");

            migrationBuilder.DropTable(name: "CatalogItemImages");
            migrationBuilder.DropTable(name: "CatalogItemBarcodes");
            migrationBuilder.DropTable(name: "CatalogItems");
        }
    }
}
