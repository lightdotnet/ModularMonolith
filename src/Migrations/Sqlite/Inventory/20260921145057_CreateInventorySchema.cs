using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sqlite.Inventory
{
    /// <inheritdoc />
    public partial class CreateInventorySchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "inventory");

            migrationBuilder.CreateTable(
                name: "StockAdjustments",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProductId = table.Column<long>(type: "INTEGER", nullable: false),
                    LocationId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    QuantityDelta = table.Column<int>(type: "INTEGER", nullable: false),
                    Reason = table.Column<int>(type: "INTEGER", nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    OccurredAt = table.Column<long>(type: "INTEGER", nullable: false),
                    PerformedByUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    SourceType = table.Column<int>(type: "INTEGER", nullable: true),
                    SourceId = table.Column<long>(type: "INTEGER", nullable: true),
                    SourceLineId = table.Column<long>(type: "INTEGER", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ReversesAdjustmentId = table.Column<long>(type: "INTEGER", nullable: true),
                    UnitCostBase = table.Column<decimal>(type: "TEXT", precision: 19, scale: 4, nullable: false),
                    ValueDeltaBase = table.Column<decimal>(type: "TEXT", precision: 19, scale: 4, nullable: false),
                    Created = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    LastModified = table.Column<long>(type: "INTEGER", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockAdjustments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StockLevels",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProductId = table.Column<long>(type: "INTEGER", nullable: false),
                    LocationId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    QuantityOnHand = table.Column<int>(type: "INTEGER", nullable: false),
                    ConcurrencyToken = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    TotalValueBase = table.Column<decimal>(type: "TEXT", precision: 19, scale: 4, nullable: false),
                    Created = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    LastModified = table.Column<long>(type: "INTEGER", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockLevels", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustments_IdempotencyKey",
                schema: "inventory",
                table: "StockAdjustments",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustments_ProductId_LocationId",
                schema: "inventory",
                table: "StockAdjustments",
                columns: new[] { "ProductId", "LocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustments_ReversesAdjustmentId",
                schema: "inventory",
                table: "StockAdjustments",
                column: "ReversesAdjustmentId");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustments_SourceType_SourceId",
                schema: "inventory",
                table: "StockAdjustments",
                columns: new[] { "SourceType", "SourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockLevels_ProductId_LocationId",
                schema: "inventory",
                table: "StockLevels",
                columns: new[] { "ProductId", "LocationId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockAdjustments",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "StockLevels",
                schema: "inventory");
        }
    }
}
