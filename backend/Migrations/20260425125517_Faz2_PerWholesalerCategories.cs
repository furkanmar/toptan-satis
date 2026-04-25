using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WholesaleApi.Migrations
{
    /// <inheritdoc />
    public partial class Faz2_PerWholesalerCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Categories_Slug",
                table: "Categories");

            migrationBuilder.AddColumn<Guid>(
                name: "WholesalerId",
                table: "Categories",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_WholesalerId",
                table: "Categories",
                column: "WholesalerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_Wholesalers_WholesalerId",
                table: "Categories",
                column: "WholesalerId",
                principalTable: "Wholesalers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Categories_Wholesalers_WholesalerId",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Categories_WholesalerId",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "WholesalerId",
                table: "Categories");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Slug",
                table: "Categories",
                column: "Slug",
                unique: true);
        }
    }
}
