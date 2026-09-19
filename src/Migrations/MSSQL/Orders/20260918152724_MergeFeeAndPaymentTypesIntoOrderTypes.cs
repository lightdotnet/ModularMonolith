using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MSSQL.Orders
{
    /// <inheritdoc />
    public partial class MergeFeeAndPaymentTypesIntoOrderTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Create the new merged table first, so the data-preserving copy in step 2 below has
            // somewhere to land before the old tables are dropped in step 3. Composite PK (Id,
            // Category) — Id only needs to be unique within a Category, not globally (both old
            // catalogs seed a colliding "OTHER" code).
            migrationBuilder.CreateTable(
                name: "OrderTypes",
                schema: "orders",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderTypes", x => new { x.Id, x.Category });
                });

            // 2) Copy every existing row forward, not just the seed rows — this catalog is
            // admin-manageable, so a deployed system may have rows beyond OrdersContextInitialiser's
            // seed set. OrderTypeCategory.Fee = 0, OrderTypeCategory.Payment = 1 (see the enum's
            // declared order in Orders.Contracts.Common.OrderTypeCategory).
            migrationBuilder.Sql(
                """
                INSERT INTO [orders].[OrderTypes] ([Id], [Category], [Name], [Status], [Created], [CreatedBy], [LastModified], [LastModifiedBy])
                SELECT [Id], 0, [Name], [Status], [Created], [CreatedBy], [LastModified], [LastModifiedBy]
                FROM [orders].[FeeTypes];
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO [orders].[OrderTypes] ([Id], [Category], [Name], [Status], [Created], [CreatedBy], [LastModified], [LastModifiedBy])
                SELECT [Id], 1, [Name], [Status], [Created], [CreatedBy], [LastModified], [LastModifiedBy]
                FROM [orders].[PaymentTypes];
                """);

            // 3) Drop the now-superseded tables. OrderFees.FeeTypeId/Payments.PaymentTypeId are plain
            // denormalized snapshot columns (no FK) since DenormalizeFeeAndPaymentTypeNames, so this
            // does not touch either table.
            migrationBuilder.DropTable(
                name: "FeeTypes",
                schema: "orders");

            migrationBuilder.DropTable(
                name: "PaymentTypes",
                schema: "orders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FeeTypes",
                schema: "orders",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
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
                    Created = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentTypes", x => x.Id);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO [orders].[FeeTypes] ([Id], [Name], [Status], [Created], [CreatedBy], [LastModified], [LastModifiedBy])
                SELECT [Id], [Name], [Status], [Created], [CreatedBy], [LastModified], [LastModifiedBy]
                FROM [orders].[OrderTypes]
                WHERE [Category] = 0;
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO [orders].[PaymentTypes] ([Id], [Name], [Status], [Created], [CreatedBy], [LastModified], [LastModifiedBy])
                SELECT [Id], [Name], [Status], [Created], [CreatedBy], [LastModified], [LastModifiedBy]
                FROM [orders].[OrderTypes]
                WHERE [Category] = 1;
                """);

            migrationBuilder.DropTable(
                name: "OrderTypes",
                schema: "orders");
        }
    }
}
