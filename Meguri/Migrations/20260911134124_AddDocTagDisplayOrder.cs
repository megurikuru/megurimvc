using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Meguri.Migrations
{
    /// <inheritdoc />
    public partial class AddDocTagDisplayOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DocTags_Docs_DocsId",
                table: "DocTags");

            migrationBuilder.DropForeignKey(
                name: "FK_DocTags_Tags_TagsId",
                table: "DocTags");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DocTags",
                table: "DocTags");

            migrationBuilder.RenameColumn(
                name: "TagsId",
                table: "DocTags",
                newName: "TagId");

            migrationBuilder.RenameColumn(
                name: "DocsId",
                table: "DocTags",
                newName: "DocId");

            migrationBuilder.RenameIndex(
                name: "IX_DocTags_TagsId",
                table: "DocTags",
                newName: "IX_DocTags_TagId");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "DocTags",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "DocTags",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_DocTags",
                table: "DocTags",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_DocTags_DocId_TagId",
                table: "DocTags",
                columns: new[] { "DocId", "TagId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DocTags_Docs_DocId",
                table: "DocTags",
                column: "DocId",
                principalTable: "Docs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DocTags_Tags_TagId",
                table: "DocTags",
                column: "TagId",
                principalTable: "Tags",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DocTags_Docs_DocId",
                table: "DocTags");

            migrationBuilder.DropForeignKey(
                name: "FK_DocTags_Tags_TagId",
                table: "DocTags");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DocTags",
                table: "DocTags");

            migrationBuilder.DropIndex(
                name: "IX_DocTags_DocId_TagId",
                table: "DocTags");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "DocTags");

            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "DocTags");

            migrationBuilder.RenameColumn(
                name: "TagId",
                table: "DocTags",
                newName: "TagsId");

            migrationBuilder.RenameColumn(
                name: "DocId",
                table: "DocTags",
                newName: "DocsId");

            migrationBuilder.RenameIndex(
                name: "IX_DocTags_TagId",
                table: "DocTags",
                newName: "IX_DocTags_TagsId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DocTags",
                table: "DocTags",
                columns: new[] { "DocsId", "TagsId" });

            migrationBuilder.AddForeignKey(
                name: "FK_DocTags_Docs_DocsId",
                table: "DocTags",
                column: "DocsId",
                principalTable: "Docs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DocTags_Tags_TagsId",
                table: "DocTags",
                column: "TagsId",
                principalTable: "Tags",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
