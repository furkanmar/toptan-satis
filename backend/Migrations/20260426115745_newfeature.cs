using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WholesaleApi.Migrations
{
    /// <inheritdoc />
    public partial class newfeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CatalogItemBarcodes_Barcode",
                table: "CatalogItemBarcodes");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "CatalogItems",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "CatalogItemImages",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "CatalogItems",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "CatalogItemImages",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogItemBarcodes_Barcode",
                table: "CatalogItemBarcodes",
                column: "Barcode");
        }
    }
}
