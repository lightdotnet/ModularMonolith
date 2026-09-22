using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sqlite.Orders
{
    /// <inheritdoc />
    public partial class CreateOrdersSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "orders");

            migrationBuilder.CreateTable(
                name: "Orders",
                schema: "orders",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LocationId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    CurrencyCode = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false, defaultValue: "VND"),
                    MemberId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    OrderCode = table.Column<string>(type: "TEXT", maxLength: 17, nullable: false),
                    ExternalReferenceCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    DiscountKind = table.Column<int>(type: "INTEGER", nullable: true),
                    DiscountValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AmountPaidAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AmountPaidCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    PlacedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    CancelledAt = table.Column<long>(type: "INTEGER", nullable: true),
                    FulfilledAt = table.Column<long>(type: "INTEGER", nullable: true),
                    CancelledReason = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    StockReconciledAt = table.Column<long>(type: "INTEGER", nullable: true),
                    ConcurrencyToken = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Created = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    LastModified = table.Column<long>(type: "INTEGER", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrderTypes",
                schema: "orders",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Created = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    LastModified = table.Column<long>(type: "INTEGER", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderTypes", x => new { x.Id, x.Category });
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                schema: "orders",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OrderId = table.Column<long>(type: "INTEGER", nullable: false),
                    OrderCode = table.Column<string>(type: "TEXT", maxLength: 17, nullable: false),
                    AmountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AmountCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    PaymentTypeId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    PaymentTypeName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    PaidAt = table.Column<long>(type: "INTEGER", nullable: false),
                    Reference = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    RecordedByUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    IsVoided = table.Column<bool>(type: "INTEGER", nullable: false),
                    VoidedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    VoidReason = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Created = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    LastModified = table.Column<long>(type: "INTEGER", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrderFees",
                schema: "orders",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OrderId = table.Column<long>(type: "INTEGER", nullable: false),
                    OrderCode = table.Column<string>(type: "TEXT", maxLength: 17, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    AmountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AmountCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    FeeTypeId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    FeeTypeName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Created = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    LastModified = table.Column<long>(type: "INTEGER", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderFees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderFees_Orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "orders",
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderLines",
                schema: "orders",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OrderId = table.Column<long>(type: "INTEGER", nullable: false),
                    OrderCode = table.Column<string>(type: "TEXT", maxLength: 17, nullable: false),
                    ProductId = table.Column<long>(type: "INTEGER", nullable: false),
                    ProductName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Sku = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    UnitPriceAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnitPriceCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    VatRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    RequestedSalePriceAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    RequestedSalePriceCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true),
                    CatalogUnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CatalogCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true),
                    AppliedRate = table.Column<decimal>(type: "decimal(18,8)", nullable: true),
                    RateEffectiveFrom = table.Column<long>(type: "INTEGER", nullable: true),
                    Created = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    LastModified = table.Column<long>(type: "INTEGER", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderLines_Orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "orders",
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderFees_FeeTypeId",
                schema: "orders",
                table: "OrderFees",
                column: "FeeTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderFees_OrderId",
                schema: "orders",
                table: "OrderFees",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderLines_OrderId",
                schema: "orders",
                table: "OrderLines",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_LocationId",
                schema: "orders",
                table: "Orders",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderCode",
                schema: "orders",
                table: "Orders",
                column: "OrderCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Status_Created",
                schema: "orders",
                table: "Orders",
                columns: new[] { "Status", "Created" },
                filter: "[PlacedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_OrderId",
                schema: "orders",
                table: "Payments",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PaymentTypeId",
                schema: "orders",
                table: "Payments",
                column: "PaymentTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderFees",
                schema: "orders");

            migrationBuilder.DropTable(
                name: "OrderLines",
                schema: "orders");

            migrationBuilder.DropTable(
                name: "OrderTypes",
                schema: "orders");

            migrationBuilder.DropTable(
                name: "Payments",
                schema: "orders");

            migrationBuilder.DropTable(
                name: "Orders",
                schema: "orders");
        }
    }
}
