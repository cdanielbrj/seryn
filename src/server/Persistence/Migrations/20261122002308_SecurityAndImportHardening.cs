using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Seryn.Server.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SecurityAndImportHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "CreatedAtUnix",
                table: "sessions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "LastUsedAtUnix",
                table: "sessions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.Sql("UPDATE sessions SET CreatedAtUnix = MAX(0, ExpiresAtUnix - 28800), LastUsedAtUnix = MAX(0, ExpiresAtUnix - 28800);");

            migrationBuilder.CreateTable(
                name: "workbook_import_previews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SessionId = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    WorkbookHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CurrentDataHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ReplaceData = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExpiresAtUnix = table.Column<long>(type: "INTEGER", nullable: false),
                    ConsumedAtUnix = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workbook_import_previews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_workbook_import_previews_sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_workbook_import_previews_ExpiresAtUnix",
                table: "workbook_import_previews",
                column: "ExpiresAtUnix");

            migrationBuilder.CreateIndex(
                name: "IX_workbook_import_previews_SessionId",
                table: "workbook_import_previews",
                column: "SessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "workbook_import_previews");

            migrationBuilder.DropColumn(
                name: "CreatedAtUnix",
                table: "sessions");

            migrationBuilder.DropColumn(
                name: "LastUsedAtUnix",
                table: "sessions");
        }
    }
}
