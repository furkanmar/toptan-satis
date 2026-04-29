using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WholesaleApi.Migrations
{
    /// <inheritdoc />
    public partial class AddFaz5_CreditAllocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CreditTransactions_StoreId",
                table: "CreditTransactions");

            migrationBuilder.AddColumn<decimal>(
                name: "AllocatedAmount",
                table: "CreditTransactions",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsFullyAllocated",
                table: "CreditTransactions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastOverdueNotifiedAt",
                table: "CreditTransactions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "CreditTransactions",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateTable(
                name: "PaymentAllocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentTransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    DebitTransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AllocatedAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentAllocations_CreditTransactions_DebitTransactionId",
                        column: x => x.DebitTransactionId,
                        principalTable: "CreditTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentAllocations_CreditTransactions_PaymentTransactionId",
                        column: x => x.PaymentTransactionId,
                        principalTable: "CreditTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CreditTransactions_StoreWholesaler_Allocation",
                table: "CreditTransactions",
                columns: new[] { "StoreId", "WholesalerId", "IsFullyAllocated", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAllocations_DebitTransactionId",
                table: "PaymentAllocations",
                column: "DebitTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAllocations_PaymentTransactionId",
                table: "PaymentAllocations",
                column: "PaymentTransactionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentAllocations");

            migrationBuilder.DropIndex(
                name: "IX_CreditTransactions_StoreWholesaler_Allocation",
                table: "CreditTransactions");

            migrationBuilder.DropColumn(
                name: "AllocatedAmount",
                table: "CreditTransactions");

            migrationBuilder.DropColumn(
                name: "IsFullyAllocated",
                table: "CreditTransactions");

            migrationBuilder.DropColumn(
                name: "LastOverdueNotifiedAt",
                table: "CreditTransactions");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "CreditTransactions");

            migrationBuilder.CreateIndex(
                name: "IX_CreditTransactions_StoreId",
                table: "CreditTransactions",
                column: "StoreId");
        }
    }
}
