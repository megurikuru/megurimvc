using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Meguri.Migrations
{
    /// <inheritdoc />
    public partial class RenameCategoryIdToFandomId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Docs_Fandoms_CategoryId",
                table: "Docs");

            migrationBuilder.RenameColumn(
                name: "CategoryId",
                table: "Docs",
                newName: "FandomId");

            migrationBuilder.RenameIndex(
                name: "IX_Docs_CategoryId",
                table: "Docs",
                newName: "IX_Docs_FandomId");

            migrationBuilder.AddForeignKey(
                name: "FK_Docs_Fandoms_FandomId",
                table: "Docs",
                column: "FandomId",
                principalTable: "Fandoms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Docs_Fandoms_FandomId",
                table: "Docs");

            migrationBuilder.RenameColumn(
                name: "FandomId",
                table: "Docs",
                newName: "CategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_Docs_FandomId",
                table: "Docs",
                newName: "IX_Docs_CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Docs_Fandoms_CategoryId",
                table: "Docs",
                column: "CategoryId",
                principalTable: "Fandoms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
