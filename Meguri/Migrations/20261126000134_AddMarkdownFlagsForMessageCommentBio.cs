using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Meguri.Migrations
{
    /// <inheritdoc />
    public partial class AddMarkdownFlagsForMessageCommentBio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "UseMarkdown",
                schema: "public",
                table: "Messages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "UseMarkdown",
                schema: "public",
                table: "Comments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "UseMarkdownBio",
                schema: "public",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UseMarkdown",
                schema: "public",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "UseMarkdown",
                schema: "public",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "UseMarkdownBio",
                schema: "public",
                table: "AspNetUsers");
        }
    }
}
