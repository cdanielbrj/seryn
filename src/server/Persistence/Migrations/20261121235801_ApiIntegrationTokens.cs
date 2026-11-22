using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Seryn.Server.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ApiIntegrationTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "api_tokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    TokenHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    DisplayPrefix = table.Column<string>(type: "TEXT", maxLength: 24, nullable: false),
                    Scopes = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CreatedAtUnix = table.Column<long>(type: "INTEGER", nullable: false),
                    ExpiresAtUnix = table.Column<long>(type: "INTEGER", nullable: true),
                    LastUsedAtUnix = table.Column<long>(type: "INTEGER", nullable: true),
                    RevokedAtUnix = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_api_tokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "idempotency_records",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApiTokenId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Key = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    RequestHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    StatusCode = table.Column<int>(type: "INTEGER", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    ResponseBody = table.Column<byte[]>(type: "BLOB", nullable: false),
                    ExpiresAtUnix = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_idempotency_records", x => x.Id);
                    table.ForeignKey(
                        name: "FK_idempotency_records_api_tokens_ApiTokenId",
                        column: x => x.ApiTokenId,
                        principalTable: "api_tokens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_api_tokens_RevokedAtUnix",
                table: "api_tokens",
                column: "RevokedAtUnix");

            migrationBuilder.CreateIndex(
                name: "IX_api_tokens_TokenHash",
                table: "api_tokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_idempotency_records_ApiTokenId_Key",
                table: "idempotency_records",
                columns: new[] { "ApiTokenId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_idempotency_records_ExpiresAtUnix",
                table: "idempotency_records",
                column: "ExpiresAtUnix");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "idempotency_records");

            migrationBuilder.DropTable(
                name: "api_tokens");
        }
    }
}
