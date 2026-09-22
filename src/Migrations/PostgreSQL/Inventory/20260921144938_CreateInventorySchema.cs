using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PostgreSQL.Inventory
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
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    LocationId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    QuantityDelta = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<int>(type: "integer", nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PerformedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    SourceType = table.Column<int>(type: "integer", nullable: true),
                    SourceId = table.Column<long>(type: "bigint", nullable: true),
                    SourceLineId = table.Column<long>(type: "bigint", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ReversesAdjustmentId = table.Column<long>(type: "bigint", nullable: true),
                    UnitCostBase = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false),
                    ValueDeltaBase = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
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
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    LocationId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    QuantityOnHand = table.Column<int>(type: "integer", nullable: false),
                    ConcurrencyToken = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TotalValueBase = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
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
