using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Meguri.Migrations
{
    /// <inheritdoc />
    public partial class AddFandomUserManyToMany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FandomUsers",
                columns: table => new
                {
                    FandomId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FandomUsers", x => new { x.FandomId, x.UserId });
                    table.ForeignKey(
                        name: "FK_FandomUsers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FandomUsers_Fandoms_FandomId",
                        column: x => x.FandomId,
                        principalTable: "Fandoms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FandomUsers_UserId",
                table: "FandomUsers",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FandomUsers");
        }
    }
}
