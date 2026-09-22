using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sqlite.Currency
{
    /// <inheritdoc />
    public partial class CreateCurrencySchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "currency");

            migrationBuilder.CreateTable(
                name: "Currencies",
                schema: "currency",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Symbol = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    DecimalPlaces = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsBase = table.Column<bool>(type: "INTEGER", nullable: false),
                    Created = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    LastModified = table.Column<long>(type: "INTEGER", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Currencies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExchangeRates",
                schema: "currency",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CurrencyCode = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    Rate = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    EffectiveFrom = table.Column<long>(type: "INTEGER", nullable: false),
                    RecordedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Created = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    LastModified = table.Column<long>(type: "INTEGER", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExchangeRates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExchangeRates_Currencies_CurrencyCode",
                        column: x => x.CurrencyCode,
                        principalSchema: "currency",
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Currencies_IsBase",
                schema: "currency",
                table: "Currencies",
                column: "IsBase",
                unique: true,
                filter: "[IsBase] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRates_CurrencyCode_EffectiveFrom",
                schema: "currency",
                table: "ExchangeRates",
                columns: new[] { "CurrencyCode", "EffectiveFrom" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExchangeRates",
                schema: "currency");

            migrationBuilder.DropTable(
                name: "Currencies",
                schema: "currency");
        }
    }
}
