using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MSSQL.Orders
{
    /// <inheritdoc />
    public partial class AddFeeTypeAndPaymentTypeCatalogs : Migration
    {
        private static readonly DateTimeOffset SeededAt = new(2026, 9, 18, 0, 0, 0, TimeSpan.Zero);

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Create the new catalog tables and seed them first, so the FK backfill below (step 3)
            // has rows to point at even before OrdersContextInitialiser.SeedAsync ever runs.
            migrationBuilder.CreateTable(
                name: "FeeTypes",
                schema: "orders",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeeTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentTypes",
                schema: "orders",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentTypes", x => x.Id);
                });

            migrationBuilder.InsertData(
                schema: "orders",
                table: "FeeTypes",
                columns: new[] { "Id", "Name", "Status", "Created" },
                values: new object[,]
                {
                    { "SHIPPING", "Shipping", 0, SeededAt },
                    { "OTHER", "Other", 0, SeededAt },
                });

            migrationBuilder.InsertData(
                schema: "orders",
                table: "PaymentTypes",
                columns: new[] { "Id", "Name", "Status", "Created" },
                values: new object[,]
                {
                    { "CASH", "Cash", 0, SeededAt },
                    { "CARD", "Card", 0, SeededAt },
                    { "BANK_TRANSFER", "Bank transfer", 0, SeededAt },
                    { "OTHER", "Other", 0, SeededAt },
                });

            // 2) Add the new FK columns as nullable first — the backfill in step 3 populates them
            // before step 4 tightens them to NOT NULL.
            migrationBuilder.AddColumn<string>(
                name: "FeeTypeId",
                schema: "orders",
                table: "OrderFees",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentTypeId",
                schema: "orders",
                table: "Payments",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            // 3) Backfill from the old int-enum columns to the new string FK codes.
            migrationBuilder.Sql(
                """
                UPDATE [orders].[OrderFees]
                SET [FeeTypeId] = CASE [Type]
                    WHEN 0 THEN 'SHIPPING'
                    WHEN 1 THEN 'OTHER'
                    ELSE 'OTHER'
                END;
                """);

            migrationBuilder.Sql(
                """
                UPDATE [orders].[Payments]
                SET [PaymentTypeId] = CASE [Method]
                    WHEN 0 THEN 'CASH'
                    WHEN 1 THEN 'CARD'
                    WHEN 2 THEN 'BANK_TRANSFER'
                    WHEN 3 THEN 'OTHER'
                    ELSE 'OTHER'
                END;
                """);

            // 4) Tighten to NOT NULL and add the FK constraints + indexes.
            migrationBuilder.AlterColumn<string>(
                name: "FeeTypeId",
                schema: "orders",
                table: "OrderFees",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PaymentTypeId",
                schema: "orders",
                table: "Payments",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderFees_FeeTypeId",
                schema: "orders",
                table: "OrderFees",
                column: "FeeTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PaymentTypeId",
                schema: "orders",
                table: "Payments",
                column: "PaymentTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderFees_FeeTypes_FeeTypeId",
                schema: "orders",
                table: "OrderFees",
                column: "FeeTypeId",
                principalSchema: "orders",
                principalTable: "FeeTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_PaymentTypes_PaymentTypeId",
                schema: "orders",
                table: "Payments",
                column: "PaymentTypeId",
                principalSchema: "orders",
                principalTable: "PaymentTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // 5) Drop the now-superseded int-enum columns.
            migrationBuilder.DropColumn(
                name: "Type",
                schema: "orders",
                table: "OrderFees");

            migrationBuilder.DropColumn(
                name: "Method",
                schema: "orders",
                table: "Payments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Type",
                schema: "orders",
                table: "OrderFees",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Method",
                schema: "orders",
                table: "Payments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.DropForeignKey(
                name: "FK_OrderFees_FeeTypes_FeeTypeId",
                schema: "orders",
                table: "OrderFees");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_PaymentTypes_PaymentTypeId",
                schema: "orders",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_OrderFees_FeeTypeId",
                schema: "orders",
                table: "OrderFees");

            migrationBuilder.DropIndex(
                name: "IX_Payments_PaymentTypeId",
                schema: "orders",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "FeeTypeId",
                schema: "orders",
                table: "OrderFees");

            migrationBuilder.DropColumn(
                name: "PaymentTypeId",
                schema: "orders",
                table: "Payments");

            migrationBuilder.DropTable(
                name: "FeeTypes",
                schema: "orders");

            migrationBuilder.DropTable(
                name: "PaymentTypes",
                schema: "orders");
        }
    }
}
