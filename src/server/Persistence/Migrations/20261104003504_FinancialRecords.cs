using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Seryn.Server.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FinancialRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "financial_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Direction = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    AmountCents = table.Column<long>(type: "INTEGER", nullable: false),
                    StartYear = table.Column<int>(type: "INTEGER", nullable: false),
                    StartMonth = table.Column<int>(type: "INTEGER", nullable: false),
                    Recurrence = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    OccurrenceCount = table.Column<int>(type: "INTEGER", nullable: true),
                    TypeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CategoryId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PaymentMethodId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAtUnix = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAtUnix = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_financial_records", x => x.Id);
                    table.CheckConstraint("record_amount_positive", "AmountCents > 0");
                    table.CheckConstraint("record_occurrence_count", "OccurrenceCount IS NULL OR OccurrenceCount >= 1");
                    table.CheckConstraint("record_start_month", "StartMonth >= 1 AND StartMonth <= 12");
                    table.CheckConstraint("record_start_year", "StartYear >= 1 AND StartYear <= 9999");
                    table.ForeignKey(
                        name: "FK_financial_records_financial_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "financial_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_financial_records_financial_types_TypeId",
                        column: x => x.TypeId,
                        principalTable: "financial_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_financial_records_payment_methods_PaymentMethodId",
                        column: x => x.PaymentMethodId,
                        principalTable: "payment_methods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_financial_records_CategoryId",
                table: "financial_records",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_financial_records_PaymentMethodId",
                table: "financial_records",
                column: "PaymentMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_financial_records_StartYear_StartMonth",
                table: "financial_records",
                columns: new[] { "StartYear", "StartMonth" });

            migrationBuilder.CreateIndex(
                name: "IX_financial_records_TypeId",
                table: "financial_records",
                column: "TypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "financial_records");
        }
    }
}
