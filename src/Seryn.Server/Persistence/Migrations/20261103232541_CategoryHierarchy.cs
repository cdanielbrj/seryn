using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Seryn.Server.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CategoryHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_financial_categories_NormalizedName",
                table: "financial_categories");

            migrationBuilder.AddColumn<Guid>(
                name: "ParentId",
                table: "financial_categories",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScopeKey",
                table: "financial_categories",
                type: "TEXT",
                maxLength: 32,
                nullable: false,
                defaultValue: "ROOT");

            migrationBuilder.CreateIndex(
                name: "IX_financial_categories_ParentId",
                table: "financial_categories",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_financial_categories_ScopeKey_NormalizedName",
                table: "financial_categories",
                columns: new[] { "ScopeKey", "NormalizedName" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_financial_categories_financial_categories_ParentId",
                table: "financial_categories",
                column: "ParentId",
                principalTable: "financial_categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_financial_categories_financial_categories_ParentId",
                table: "financial_categories");

            migrationBuilder.DropIndex(
                name: "IX_financial_categories_ParentId",
                table: "financial_categories");

            migrationBuilder.DropIndex(
                name: "IX_financial_categories_ScopeKey_NormalizedName",
                table: "financial_categories");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "financial_categories");

            migrationBuilder.DropColumn(
                name: "ScopeKey",
                table: "financial_categories");

            migrationBuilder.CreateIndex(
                name: "IX_financial_categories_NormalizedName",
                table: "financial_categories",
                column: "NormalizedName",
                unique: true);
        }
    }
}
