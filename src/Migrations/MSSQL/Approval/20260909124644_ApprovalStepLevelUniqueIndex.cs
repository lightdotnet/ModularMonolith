using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MSSQL.Approval
{
    /// <inheritdoc />
    public partial class ApprovalStepLevelUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ApprovalSteps_ApprovalRequestId",
                schema: "approval",
                table: "ApprovalSteps");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalSteps_ApprovalRequestId_Level",
                schema: "approval",
                table: "ApprovalSteps",
                columns: new[] { "ApprovalRequestId", "Level" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ApprovalSteps_ApprovalRequestId_Level",
                schema: "approval",
                table: "ApprovalSteps");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalSteps_ApprovalRequestId",
                schema: "approval",
                table: "ApprovalSteps",
                column: "ApprovalRequestId");
        }
    }
}
