using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Seryn.Server.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Foundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "local_user",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Username = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    FailedAttempts = table.Column<int>(type: "INTEGER", nullable: false),
                    LockedUntilUnix = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_local_user", x => x.Id);
                    table.CheckConstraint("single_user", "Id = 1");
                });

            migrationBuilder.CreateTable(
                name: "sessions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ExpiresAtUnix = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sessions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_sessions_ExpiresAtUnix",
                table: "sessions",
                column: "ExpiresAtUnix");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "local_user");

            migrationBuilder.DropTable(
                name: "sessions");
        }
    }
}
