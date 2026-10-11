using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Meguri.Migrations
{
    /// <inheritdoc />
    public partial class RemoveImageVisibilityColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Images_IsPublic",
                table: "Images");

            migrationBuilder.DropIndex(
                name: "IX_Images_IsPublic_CreatedAt",
                table: "Images");

            migrationBuilder.DropColumn(
                name: "IsPublic",
                table: "Images");

            migrationBuilder.DropColumn(
                name: "IsSexual",
                table: "Images");

            migrationBuilder.DropColumn(
                name: "IsViolence",
                table: "Images");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Images",
                newName: "Figcaption");

            migrationBuilder.RenameColumn(
                name: "Caption",
                table: "Images",
                newName: "Alt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Figcaption",
                table: "Images",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "Alt",
                table: "Images",
                newName: "Caption");

            migrationBuilder.AddColumn<bool>(
                name: "IsPublic",
                table: "Images",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSexual",
                table: "Images",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsViolence",
                table: "Images",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Images_IsPublic",
                table: "Images",
                column: "IsPublic");

            migrationBuilder.CreateIndex(
                name: "IX_Images_IsPublic_CreatedAt",
                table: "Images",
                columns: new[] { "IsPublic", "CreatedAt" });
        }
    }
}
