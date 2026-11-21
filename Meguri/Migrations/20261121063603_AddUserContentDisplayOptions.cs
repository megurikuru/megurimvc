using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Meguri.Migrations
{
    /// <inheritdoc />
    public partial class AddUserContentDisplayOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ShowSexual",
                schema: "public",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowViolence",
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
                name: "ShowSexual",
                schema: "public",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ShowViolence",
                schema: "public",
                table: "AspNetUsers");
        }
    }
}
