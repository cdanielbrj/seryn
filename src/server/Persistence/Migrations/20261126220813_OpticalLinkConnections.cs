using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Seryn.Server.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OpticalLinkConnections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "optical_link_connections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Provider = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Model = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    ProtectedApiKey = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: false),
                    KeyHint = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUnix = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAtUnix = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_optical_link_connections", x => x.Id);
                    table.CheckConstraint("optical_link_default", "IsDefault IN (0, 1)");
                    table.CheckConstraint("optical_link_provider", "Provider = 'openrouter'");
                });

            migrationBuilder.CreateIndex(
                name: "IX_optical_link_connections_IsDefault",
                table: "optical_link_connections",
                column: "IsDefault",
                unique: true,
                filter: "IsDefault = 1");

            migrationBuilder.CreateIndex(
                name: "IX_optical_link_connections_Name",
                table: "optical_link_connections",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "optical_link_connections");
        }
    }
}
