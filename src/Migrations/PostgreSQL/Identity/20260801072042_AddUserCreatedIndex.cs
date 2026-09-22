using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PostgreSQL.Identity
{
    /// <inheritdoc />
    public partial class AddUserCreatedIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Users_Created",
                schema: "identity",
                table: "Users",
                column: "Created");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Created",
                schema: "identity",
                table: "Users");
        }
    }
}
