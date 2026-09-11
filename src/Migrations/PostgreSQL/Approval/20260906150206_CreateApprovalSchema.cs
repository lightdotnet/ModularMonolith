using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PostgreSQL.Approval
{
    /// <inheritdoc />
    public partial class CreateApprovalSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "approval");

            migrationBuilder.CreateTable(
                name: "ApprovalDocumentTypes",
                schema: "approval",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalDocumentTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalRequests",
                schema: "approval",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    RequestType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RequestId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    RequesterUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    RequesterEmployeeId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    RequesterName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Title = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Content = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    DeepLinkUrl = table.Column<string>(type: "text", nullable: true),
                    DocumentTypeId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    CurrentLevel = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    FinalizedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalRequests_ApprovalDocumentTypes_DocumentTypeId",
                        column: x => x.DocumentTypeId,
                        principalSchema: "approval",
                        principalTable: "ApprovalDocumentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalSteps",
                schema: "approval",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    ApprovalRequestId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    ApproverUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    ApproverEmployeeId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    ApproverName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DecidedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalSteps_ApprovalRequests_ApprovalRequestId",
                        column: x => x.ApprovalRequestId,
                        principalSchema: "approval",
                        principalTable: "ApprovalRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalDocumentTypes_Code",
                schema: "approval",
                table: "ApprovalDocumentTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_DocumentTypeId",
                schema: "approval",
                table: "ApprovalRequests",
                column: "DocumentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_RequesterUserId",
                schema: "approval",
                table: "ApprovalRequests",
                column: "RequesterUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_RequestType_RequestId",
                schema: "approval",
                table: "ApprovalRequests",
                columns: new[] { "RequestType", "RequestId" });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalSteps_ApprovalRequestId",
                schema: "approval",
                table: "ApprovalSteps",
                column: "ApprovalRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalSteps_ApproverUserId",
                schema: "approval",
                table: "ApprovalSteps",
                column: "ApproverUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApprovalSteps",
                schema: "approval");

            migrationBuilder.DropTable(
                name: "ApprovalRequests",
                schema: "approval");

            migrationBuilder.DropTable(
                name: "ApprovalDocumentTypes",
                schema: "approval");
        }
    }
}
