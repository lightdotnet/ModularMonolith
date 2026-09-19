using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MSSQL.Orders
{
    /// <inheritdoc />
    public partial class DenormalizeFeeAndPaymentTypeNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Drop the FK constraints — FeeTypeId/PaymentTypeId become plain denormalized ids
            // (no FK), same treatment as OrderFees.OrderCode/Payments.OrderCode. The indexes on
            // FeeTypeId/PaymentTypeId are kept.
            migrationBuilder.DropForeignKey(
                name: "FK_OrderFees_FeeTypes_FeeTypeId",
                schema: "orders",
                table: "OrderFees");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_PaymentTypes_PaymentTypeId",
                schema: "orders",
                table: "Payments");

            // 2) Add the new snapshot columns as nullable first — the backfill in step 3 populates
            // them before step 4 tightens them to NOT NULL.
            migrationBuilder.AddColumn<string>(
                name: "FeeTypeName",
                schema: "orders",
                table: "OrderFees",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentTypeName",
                schema: "orders",
                table: "Payments",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            // 3) One-time backfill of existing rows from whatever the catalog currently says for
            // that FeeTypeId/PaymentTypeId.
            migrationBuilder.Sql(
                """
                UPDATE f
                SET f.[FeeTypeName] = ft.[Name]
                FROM [orders].[OrderFees] f
                JOIN [orders].[FeeTypes] ft ON ft.[Id] = f.[FeeTypeId];
                """);

            migrationBuilder.Sql(
                """
                UPDATE p
                SET p.[PaymentTypeName] = pt.[Name]
                FROM [orders].[Payments] p
                JOIN [orders].[PaymentTypes] pt ON pt.[Id] = p.[PaymentTypeId];
                """);

            // 4) Tighten to NOT NULL now that every row has been backfilled.
            migrationBuilder.AlterColumn<string>(
                name: "FeeTypeName",
                schema: "orders",
                table: "OrderFees",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PaymentTypeName",
                schema: "orders",
                table: "Payments",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FeeTypeName",
                schema: "orders",
                table: "OrderFees");

            migrationBuilder.DropColumn(
                name: "PaymentTypeName",
                schema: "orders",
                table: "Payments");

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
        }
    }
}
