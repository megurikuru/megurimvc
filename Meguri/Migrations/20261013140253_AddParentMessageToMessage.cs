using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Meguri.Migrations
{
    /// <inheritdoc />
    public partial class AddParentMessageToMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ParentMessageId",
                table: "Messages",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ParentMessageId",
                table: "Messages",
                column: "ParentMessageId");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Messages_ParentMessageId",
                table: "Messages",
                column: "ParentMessageId",
                principalTable: "Messages",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Messages_ParentMessageId",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_ParentMessageId",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "ParentMessageId",
                table: "Messages");
        }
    }
}
