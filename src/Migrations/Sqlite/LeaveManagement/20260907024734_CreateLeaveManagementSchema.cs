using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sqlite.LeaveManagement
{
    /// <inheritdoc />
    public partial class CreateLeaveManagementSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "leave");

            migrationBuilder.CreateTable(
                name: "LeaveRequests",
                schema: "leave",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    EmployeeId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    LeaveType = table.Column<int>(type: "INTEGER", nullable: false),
                    StartDate = table.Column<long>(type: "INTEGER", nullable: false),
                    EndDate = table.Column<long>(type: "INTEGER", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ApprovalRequestId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    Created = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    LastModified = table.Column<long>(type: "INTEGER", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveRequests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_EmployeeId_Status",
                schema: "leave",
                table: "LeaveRequests",
                columns: new[] { "EmployeeId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LeaveRequests",
                schema: "leave");
        }
    }
}
