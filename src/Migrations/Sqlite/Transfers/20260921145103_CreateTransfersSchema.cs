using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sqlite.Transfers
{
    /// <inheritdoc />
    public partial class CreateTransfersSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "transfers");

            migrationBuilder.CreateTable(
                name: "StockTransfers",
                schema: "transfers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TransferCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    SourceLocationId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    SourceLocationName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    DestinationLocationId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    DestinationLocationName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    RequestedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    PostingStartedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    DispatchedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    ReceivedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    ClosedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    ClosedReason = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CancelledAt = table.Column<long>(type: "INTEGER", nullable: true),
                    CancelledReason = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ConcurrencyToken = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Created = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    LastModified = table.Column<long>(type: "INTEGER", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTransfers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TransferLines",
                schema: "transfers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TransferId = table.Column<long>(type: "INTEGER", nullable: false),
                    ProductId = table.Column<long>(type: "INTEGER", nullable: false),
                    ProductName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Sku = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    RequestedQuantity = table.Column<int>(type: "INTEGER", nullable: false),
                    QtyDispatched = table.Column<int>(type: "INTEGER", nullable: false),
                    QtyReceived = table.Column<int>(type: "INTEGER", nullable: false),
                    QtyClosedShort = table.Column<int>(type: "INTEGER", nullable: false),
                    UnitCostBase = table.Column<decimal>(type: "TEXT", precision: 19, scale: 4, nullable: false),
                    Created = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    LastModified = table.Column<long>(type: "INTEGER", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransferLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransferLines_StockTransfers_TransferId",
                        column: x => x.TransferId,
                        principalSchema: "transfers",
                        principalTable: "StockTransfers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TransferReceipts",
                schema: "transfers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TransferId = table.Column<long>(type: "INTEGER", nullable: false),
                    ClientRequestId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ReceivedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    VoidedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    VoidReason = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Created = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    LastModified = table.Column<long>(type: "INTEGER", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransferReceipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransferReceipts_StockTransfers_TransferId",
                        column: x => x.TransferId,
                        principalSchema: "transfers",
                        principalTable: "StockTransfers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TransferReceiptLines",
                schema: "transfers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ReceiptId = table.Column<long>(type: "INTEGER", nullable: false),
                    TransferLineId = table.Column<long>(type: "INTEGER", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    Created = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    LastModified = table.Column<long>(type: "INTEGER", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransferReceiptLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransferReceiptLines_TransferReceipts_ReceiptId",
                        column: x => x.ReceiptId,
                        principalSchema: "transfers",
                        principalTable: "TransferReceipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_DestinationLocationId",
                schema: "transfers",
                table: "StockTransfers",
                column: "DestinationLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_PostingStartedAt",
                schema: "transfers",
                table: "StockTransfers",
                column: "PostingStartedAt",
                filter: "[PostingStartedAt] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_SourceLocationId",
                schema: "transfers",
                table: "StockTransfers",
                column: "SourceLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_Status_Created",
                schema: "transfers",
                table: "StockTransfers",
                columns: new[] { "Status", "Created" });

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_TransferCode",
                schema: "transfers",
                table: "StockTransfers",
                column: "TransferCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransferLines_ProductId",
                schema: "transfers",
                table: "TransferLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferLines_TransferId",
                schema: "transfers",
                table: "TransferLines",
                column: "TransferId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferReceiptLines_ReceiptId",
                schema: "transfers",
                table: "TransferReceiptLines",
                column: "ReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferReceipts_Status_Created",
                schema: "transfers",
                table: "TransferReceipts",
                columns: new[] { "Status", "Created" });

            migrationBuilder.CreateIndex(
                name: "IX_TransferReceipts_TransferId_ClientRequestId",
                schema: "transfers",
                table: "TransferReceipts",
                columns: new[] { "TransferId", "ClientRequestId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TransferLines",
                schema: "transfers");

            migrationBuilder.DropTable(
                name: "TransferReceiptLines",
                schema: "transfers");

            migrationBuilder.DropTable(
                name: "TransferReceipts",
                schema: "transfers");

            migrationBuilder.DropTable(
                name: "StockTransfers",
                schema: "transfers");
        }
    }
}
