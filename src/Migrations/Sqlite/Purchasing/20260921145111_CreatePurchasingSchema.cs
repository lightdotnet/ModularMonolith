using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sqlite.Purchasing
{
    /// <inheritdoc />
    public partial class CreatePurchasingSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "purchasing");

            migrationBuilder.CreateTable(
                name: "Suppliers",
                schema: "purchasing",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ContactName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Address = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    PaymentTerms = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Created = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    LastModified = table.Column<long>(type: "INTEGER", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrders",
                schema: "purchasing",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PONumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    SupplierId = table.Column<long>(type: "INTEGER", nullable: false),
                    SupplierName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    LocationId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    LocationName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ExpectedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    RequesterUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    RequesterEmployeeId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    ApproverEmployeeId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    ApproverName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ApprovalRequestId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    SubmittedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    ApprovedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    RejectedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    ReceivedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    ClosedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    ClosedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    ClosedReason = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CancelledAt = table.Column<long>(type: "INTEGER", nullable: true),
                    CancelledBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    CancelledReason = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ConcurrencyToken = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Created = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    LastModified = table.Column<long>(type: "INTEGER", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalSchema: "purchasing",
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GoodsReceipts",
                schema: "purchasing",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ReceiptNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    PurchaseOrderId = table.Column<long>(type: "INTEGER", nullable: false),
                    SupplierId = table.Column<long>(type: "INTEGER", nullable: false),
                    SupplierName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    LocationId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    LocationName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    DeliveryNoteRef = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ReceivedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    ReceivedByUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    StockPostedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    VoidedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    VoidReason = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ConcurrencyToken = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Created = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    LastModified = table.Column<long>(type: "INTEGER", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodsReceipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoodsReceipts_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalSchema: "purchasing",
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrderLines",
                schema: "purchasing",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PurchaseOrderId = table.Column<long>(type: "INTEGER", nullable: false),
                    ProductId = table.Column<long>(type: "INTEGER", nullable: false),
                    ProductName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Sku = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    OrderedQuantity = table.Column<int>(type: "INTEGER", nullable: false),
                    ReceivedQuantity = table.Column<int>(type: "INTEGER", nullable: false),
                    ReturnedQuantity = table.Column<int>(type: "INTEGER", nullable: false),
                    UnitCost = table.Column<decimal>(type: "TEXT", precision: 19, scale: 4, nullable: false),
                    Created = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    LastModified = table.Column<long>(type: "INTEGER", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrderLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderLines_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalSchema: "purchasing",
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GoodsReceiptLines",
                schema: "purchasing",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GoodsReceiptId = table.Column<long>(type: "INTEGER", nullable: false),
                    PurchaseOrderLineId = table.Column<long>(type: "INTEGER", nullable: false),
                    ProductId = table.Column<long>(type: "INTEGER", nullable: false),
                    ProductName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Sku = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    UnitCostBase = table.Column<decimal>(type: "TEXT", precision: 19, scale: 4, nullable: false),
                    Created = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    LastModified = table.Column<long>(type: "INTEGER", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodsReceiptLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoodsReceiptLines_GoodsReceipts_GoodsReceiptId",
                        column: x => x.GoodsReceiptId,
                        principalSchema: "purchasing",
                        principalTable: "GoodsReceipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseReturns",
                schema: "purchasing",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ReturnNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    SupplierId = table.Column<long>(type: "INTEGER", nullable: false),
                    SupplierName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    GoodsReceiptId = table.Column<long>(type: "INTEGER", nullable: false),
                    PurchaseOrderId = table.Column<long>(type: "INTEGER", nullable: false),
                    LocationId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    LocationName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Reason = table.Column<int>(type: "INTEGER", nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    PostingStartedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    PostedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    PostedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    ExpectedCreditBase = table.Column<decimal>(type: "TEXT", precision: 19, scale: 4, nullable: true),
                    CreditNoteNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CreditAmountBase = table.Column<decimal>(type: "TEXT", precision: 19, scale: 4, nullable: true),
                    CreditedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    CreditedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    CancelledAt = table.Column<long>(type: "INTEGER", nullable: true),
                    CancelledBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    CancelledReason = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ConcurrencyToken = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Created = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    LastModified = table.Column<long>(type: "INTEGER", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseReturns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseReturns_GoodsReceipts_GoodsReceiptId",
                        column: x => x.GoodsReceiptId,
                        principalSchema: "purchasing",
                        principalTable: "GoodsReceipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseReturnLines",
                schema: "purchasing",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PurchaseReturnId = table.Column<long>(type: "INTEGER", nullable: false),
                    GoodsReceiptLineId = table.Column<long>(type: "INTEGER", nullable: false),
                    PurchaseOrderLineId = table.Column<long>(type: "INTEGER", nullable: false),
                    ProductId = table.Column<long>(type: "INTEGER", nullable: false),
                    ProductName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Sku = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    ReceiptUnitCostBase = table.Column<decimal>(type: "TEXT", precision: 19, scale: 4, nullable: false),
                    CostRemovedBase = table.Column<decimal>(type: "TEXT", precision: 19, scale: 4, nullable: true),
                    Reason = table.Column<int>(type: "INTEGER", nullable: true),
                    Created = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    LastModified = table.Column<long>(type: "INTEGER", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseReturnLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseReturnLines_PurchaseReturns_PurchaseReturnId",
                        column: x => x.PurchaseReturnId,
                        principalSchema: "purchasing",
                        principalTable: "PurchaseReturns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptLines_GoodsReceiptId",
                schema: "purchasing",
                table: "GoodsReceiptLines",
                column: "GoodsReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptLines_PurchaseOrderLineId",
                schema: "purchasing",
                table: "GoodsReceiptLines",
                column: "PurchaseOrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipts_LocationId",
                schema: "purchasing",
                table: "GoodsReceipts",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipts_PurchaseOrderId_DeliveryNoteRef",
                schema: "purchasing",
                table: "GoodsReceipts",
                columns: new[] { "PurchaseOrderId", "DeliveryNoteRef" },
                unique: true,
                filter: "[DeliveryNoteRef] IS NOT NULL AND [Status] <> 2");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipts_ReceiptNumber",
                schema: "purchasing",
                table: "GoodsReceipts",
                column: "ReceiptNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipts_Status_Created",
                schema: "purchasing",
                table: "GoodsReceipts",
                columns: new[] { "Status", "Created" });

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipts_SupplierId",
                schema: "purchasing",
                table: "GoodsReceipts",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderLines_ProductId",
                schema: "purchasing",
                table: "PurchaseOrderLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderLines_PurchaseOrderId",
                schema: "purchasing",
                table: "PurchaseOrderLines",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_LocationId",
                schema: "purchasing",
                table: "PurchaseOrders",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_PONumber",
                schema: "purchasing",
                table: "PurchaseOrders",
                column: "PONumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_Status_Created",
                schema: "purchasing",
                table: "PurchaseOrders",
                columns: new[] { "Status", "Created" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_SupplierId",
                schema: "purchasing",
                table: "PurchaseOrders",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturnLines_GoodsReceiptLineId",
                schema: "purchasing",
                table: "PurchaseReturnLines",
                column: "GoodsReceiptLineId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturnLines_PurchaseReturnId",
                schema: "purchasing",
                table: "PurchaseReturnLines",
                column: "PurchaseReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturns_GoodsReceiptId",
                schema: "purchasing",
                table: "PurchaseReturns",
                column: "GoodsReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturns_LocationId",
                schema: "purchasing",
                table: "PurchaseReturns",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturns_PostingStartedAt",
                schema: "purchasing",
                table: "PurchaseReturns",
                column: "PostingStartedAt",
                filter: "[PostingStartedAt] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturns_ReturnNumber",
                schema: "purchasing",
                table: "PurchaseReturns",
                column: "ReturnNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturns_Status_Created",
                schema: "purchasing",
                table: "PurchaseReturns",
                columns: new[] { "Status", "Created" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturns_SupplierId",
                schema: "purchasing",
                table: "PurchaseReturns",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_Code",
                schema: "purchasing",
                table: "Suppliers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_Status_Name",
                schema: "purchasing",
                table: "Suppliers",
                columns: new[] { "Status", "Name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GoodsReceiptLines",
                schema: "purchasing");

            migrationBuilder.DropTable(
                name: "PurchaseOrderLines",
                schema: "purchasing");

            migrationBuilder.DropTable(
                name: "PurchaseReturnLines",
                schema: "purchasing");

            migrationBuilder.DropTable(
                name: "PurchaseReturns",
                schema: "purchasing");

            migrationBuilder.DropTable(
                name: "GoodsReceipts",
                schema: "purchasing");

            migrationBuilder.DropTable(
                name: "PurchaseOrders",
                schema: "purchasing");

            migrationBuilder.DropTable(
                name: "Suppliers",
                schema: "purchasing");
        }
    }
}
