using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WholesaleApi.Migrations
{
    /// <inheritdoc />
    public partial class Faz6_DeliveryNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // PostgreSQL sequence for delivery note numbering
            migrationBuilder.Sql("CREATE SEQUENCE IF NOT EXISTS delivery_note_seq START WITH 1 INCREMENT BY 1;");

            // Wholesaler: add TaxNumber and CompanyAddress
            migrationBuilder.AddColumn<string>(
                name: "TaxNumber",
                table: "Wholesalers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyAddress",
                table: "Wholesalers",
                type: "text",
                nullable: true);

            // DeliveryNotes table
            migrationBuilder.CreateTable(
                name: "DeliveryNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NoteNumber = table.Column<string>(type: "text", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    WholesalerId = table.Column<Guid>(type: "uuid", nullable: false),
                    StoreId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    IssueDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    VehiclePlate = table.Column<string>(type: "text", nullable: true),
                    DriverName = table.Column<string>(type: "text", nullable: true),
                    SourceAddress = table.Column<string>(type: "text", nullable: true),
                    DestinationAddress = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Ettn = table.Column<string>(type: "text", nullable: true),
                    EInvoiceProvider = table.Column<string>(type: "text", nullable: true),
                    EInvoiceStatus = table.Column<string>(type: "text", nullable: true),
                    EInvoiceRawResponse = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CancelledAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CancelReason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryNotes_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeliveryNotes_Wholesalers_WholesalerId",
                        column: x => x.WholesalerId,
                        principalTable: "Wholesalers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeliveryNotes_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // DeliveryNoteItems table
            migrationBuilder.CreateTable(
                name: "DeliveryNoteItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeliveryNoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductName = table.Column<string>(type: "text", nullable: false),
                    ProductBrand = table.Column<string>(type: "text", nullable: true),
                    UnitType = table.Column<string>(type: "text", nullable: false),
                    ContentQty = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    VatRate = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    QuantityOrdered = table.Column<int>(type: "integer", nullable: false),
                    QuantityShipped = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryNoteItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryNoteItems_DeliveryNotes_DeliveryNoteId",
                        column: x => x.DeliveryNoteId,
                        principalTable: "DeliveryNotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Indexes
            migrationBuilder.CreateIndex(
                name: "IX_DeliveryNotes_NoteNumber",
                table: "DeliveryNotes",
                column: "NoteNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryNotes_WholesalerId_IssueDate",
                table: "DeliveryNotes",
                columns: new[] { "WholesalerId", "IssueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryNotes_OrderId",
                table: "DeliveryNotes",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryNoteItems_DeliveryNoteId",
                table: "DeliveryNoteItems",
                column: "DeliveryNoteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "DeliveryNoteItems");
            migrationBuilder.DropTable(name: "DeliveryNotes");

            migrationBuilder.DropColumn(name: "TaxNumber", table: "Wholesalers");
            migrationBuilder.DropColumn(name: "CompanyAddress", table: "Wholesalers");

            migrationBuilder.Sql("DROP SEQUENCE IF EXISTS delivery_note_seq;");
        }
    }
}
