using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MSSQL.Location
{
    /// <inheritdoc />
    public partial class AddLocationTypeEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                schema: "location",
                table: "Locations");

            migrationBuilder.AddColumn<string>(
                name: "LocationTypeId",
                schema: "location",
                table: "Locations",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "LocationTypes",
                schema: "location",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AllowedParentTypeId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CanHaveChildren = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocationTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocationTypes_LocationTypes_AllowedParentTypeId",
                        column: x => x.AllowedParentTypeId,
                        principalSchema: "location",
                        principalTable: "LocationTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Locations_LocationTypeId",
                schema: "location",
                table: "Locations",
                column: "LocationTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_LocationTypes_AllowedParentTypeId",
                schema: "location",
                table: "LocationTypes",
                column: "AllowedParentTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Locations_LocationTypes_LocationTypeId",
                schema: "location",
                table: "Locations",
                column: "LocationTypeId",
                principalSchema: "location",
                principalTable: "LocationTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Locations_LocationTypes_LocationTypeId",
                schema: "location",
                table: "Locations");

            migrationBuilder.DropTable(
                name: "LocationTypes",
                schema: "location");

            migrationBuilder.DropIndex(
                name: "IX_Locations_LocationTypeId",
                schema: "location",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "LocationTypeId",
                schema: "location",
                table: "Locations");

            migrationBuilder.AddColumn<int>(
                name: "Type",
                schema: "location",
                table: "Locations",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
