using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Meguri.Migrations
{
    /// <inheritdoc />
    public partial class RenamePostTitleTextToSubjectBody : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Posts",
                newName: "Subject");

            migrationBuilder.RenameColumn(
                name: "Text",
                table: "Posts",
                newName: "Body");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Subject",
                table: "Posts",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "Body",
                table: "Posts",
                newName: "Text");
        }
    }
}
