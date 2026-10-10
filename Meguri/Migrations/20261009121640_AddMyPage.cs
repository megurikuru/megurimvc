using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Meguri.Migrations
{
    /// <inheritdoc />
    public partial class AddMyPage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalUrls",
                table: "AspNetUsers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "HeaderImageId",
                table: "AspNetUsers",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsProfilePublic",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_HeaderImageId",
                table: "AspNetUsers",
                column: "HeaderImageId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_IsProfilePublic",
                table: "AspNetUsers",
                column: "IsProfilePublic");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Images_HeaderImageId",
                table: "AspNetUsers",
                column: "HeaderImageId",
                principalTable: "Images",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Images_HeaderImageId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_HeaderImageId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_IsProfilePublic",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ExternalUrls",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "HeaderImageId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IsProfilePublic",
                table: "AspNetUsers");
        }
    }
}
