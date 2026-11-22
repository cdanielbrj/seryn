using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Seryn.Server.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PreserveIdempotencyHeaders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ResponseHeadersJson",
                table: "idempotency_records",
                type: "TEXT",
                maxLength: 2048,
                nullable: false,
                defaultValue: "{}");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResponseHeadersJson",
                table: "idempotency_records");
        }
    }
}
