using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chronocode.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkAuthorizationArtifacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkAuthorizationArtifacts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChargeCodeId = table.Column<int>(type: "INTEGER", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    OriginalFileName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    UploadedByUserId = table.Column<string>(type: "TEXT", nullable: false),
                    UploadedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkAuthorizationArtifacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkAuthorizationArtifacts_AspNetUsers_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkAuthorizationArtifacts_ChargeCodes_ChargeCodeId",
                        column: x => x.ChargeCodeId,
                        principalTable: "ChargeCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkAuthorizationArtifacts_ChargeCodeId",
                table: "WorkAuthorizationArtifacts",
                column: "ChargeCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkAuthorizationArtifacts_UploadedByUserId",
                table: "WorkAuthorizationArtifacts",
                column: "UploadedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkAuthorizationArtifacts");
        }
    }
}
