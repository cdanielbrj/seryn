using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Seryn.Server.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Objectives : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "objectives",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    EstimatedValueCents = table.Column<long>(type: "INTEGER", nullable: false),
                    Interest = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    Link = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    DesiredYear = table.Column<int>(type: "INTEGER", nullable: true),
                    DesiredMonth = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAtUnix = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAtUnix = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_objectives", x => x.Id);
                    table.CheckConstraint("objective_desired_month", "(DesiredYear IS NULL AND DesiredMonth IS NULL) OR (DesiredYear >= 1 AND DesiredYear <= 9999 AND DesiredMonth >= 1 AND DesiredMonth <= 12)");
                    table.CheckConstraint("objective_priority_range", "Priority >= -3 AND Priority <= 3");
                    table.CheckConstraint("objective_value_positive", "EstimatedValueCents > 0");
                });

            migrationBuilder.CreateIndex(
                name: "IX_objectives_Interest_Priority",
                table: "objectives",
                columns: new[] { "Interest", "Priority" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "objectives");
        }
    }
}
