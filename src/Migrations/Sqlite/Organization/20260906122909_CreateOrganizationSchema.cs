using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sqlite.Organization
{
    /// <inheritdoc />
    public partial class CreateOrganizationSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "organization");

            migrationBuilder.CreateTable(
                name: "Companies",
                schema: "organization",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    TaxCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Address = table.Column<string>(type: "TEXT", nullable: true),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Website = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Created = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    LastModified = table.Column<long>(type: "INTEGER", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeLevels",
                schema: "organization",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    CompanyId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Rank = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Created = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    LastModified = table.Column<long>(type: "INTEGER", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeLevels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeLevels_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "organization",
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Employees",
                schema: "organization",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    CompanyId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    EmployeeCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    FirstName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DateOfBirth = table.Column<long>(type: "INTEGER", nullable: true),
                    Gender = table.Column<string>(type: "TEXT", nullable: true),
                    NationalId = table.Column<string>(type: "TEXT", nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    PhoneNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Address = table.Column<string>(type: "TEXT", nullable: true),
                    HireDate = table.Column<long>(type: "INTEGER", nullable: true),
                    TerminationDate = table.Column<long>(type: "INTEGER", nullable: true),
                    EmploymentStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    AvatarUrl = table.Column<string>(type: "TEXT", nullable: true),
                    Created = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    LastModified = table.Column<long>(type: "INTEGER", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Employees_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "organization",
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrgUnits",
                schema: "organization",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    CompanyId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    ParentId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ManagerEmployeeId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Created = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    LastModified = table.Column<long>(type: "INTEGER", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrgUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrgUnits_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "organization",
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrgUnits_OrgUnits_ParentId",
                        column: x => x.ParentId,
                        principalSchema: "organization",
                        principalTable: "OrgUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeOrgUnitMemberships",
                schema: "organization",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    EmployeeId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    OrgUnitId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    LevelId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    IsPrimary = table.Column<bool>(type: "INTEGER", nullable: false),
                    AssignmentType = table.Column<int>(type: "INTEGER", nullable: false),
                    IsManager = table.Column<bool>(type: "INTEGER", nullable: false),
                    StartDate = table.Column<long>(type: "INTEGER", nullable: false),
                    EndDate = table.Column<long>(type: "INTEGER", nullable: true),
                    Created = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    LastModified = table.Column<long>(type: "INTEGER", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeOrgUnitMemberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeOrgUnitMemberships_EmployeeLevels_LevelId",
                        column: x => x.LevelId,
                        principalSchema: "organization",
                        principalTable: "EmployeeLevels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EmployeeOrgUnitMemberships_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "organization",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeOrgUnitMemberships_OrgUnits_OrgUnitId",
                        column: x => x.OrgUnitId,
                        principalSchema: "organization",
                        principalTable: "OrgUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Companies_Code",
                schema: "organization",
                table: "Companies",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeLevels_CompanyId",
                schema: "organization",
                table: "EmployeeLevels",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeOrgUnitMemberships_EmployeeId",
                schema: "organization",
                table: "EmployeeOrgUnitMemberships",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeOrgUnitMemberships_LevelId",
                schema: "organization",
                table: "EmployeeOrgUnitMemberships",
                column: "LevelId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeOrgUnitMemberships_OrgUnitId",
                schema: "organization",
                table: "EmployeeOrgUnitMemberships",
                column: "OrgUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_CompanyId_EmployeeCode",
                schema: "organization",
                table: "Employees",
                columns: new[] { "CompanyId", "EmployeeCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_UserId",
                schema: "organization",
                table: "Employees",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrgUnits_CompanyId_ParentId",
                schema: "organization",
                table: "OrgUnits",
                columns: new[] { "CompanyId", "ParentId" });

            migrationBuilder.CreateIndex(
                name: "IX_OrgUnits_ParentId",
                schema: "organization",
                table: "OrgUnits",
                column: "ParentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeOrgUnitMemberships",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "EmployeeLevels",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "Employees",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "OrgUnits",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "Companies",
                schema: "organization");
        }
    }
}
