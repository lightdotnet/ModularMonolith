using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MSSQL.LeaveManagement
{
    /// <inheritdoc />
    public partial class AddLeaveRequestUserIdStatusIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_UserId_Status",
                schema: "leave",
                table: "LeaveRequests",
                columns: new[] { "UserId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LeaveRequests_UserId_Status",
                schema: "leave",
                table: "LeaveRequests");
        }
    }
}
