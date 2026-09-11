using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Meguri.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyTagHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TagRelations");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Tags",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Tags",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParentTagId",
                table: "Tags",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tags_Name_ParentTagId",
                table: "Tags",
                columns: new[] { "Name", "ParentTagId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tags_ParentTagId",
                table: "Tags",
                column: "ParentTagId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tags_Tags_ParentTagId",
                table: "Tags",
                column: "ParentTagId",
                principalTable: "Tags",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tags_Tags_ParentTagId",
                table: "Tags");

            migrationBuilder.DropIndex(
                name: "IX_Tags_Name_ParentTagId",
                table: "Tags");

            migrationBuilder.DropIndex(
                name: "IX_Tags_ParentTagId",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "ParentTagId",
                table: "Tags");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Tags",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Tags",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateTable(
                name: "TagRelations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RelatedTagId = table.Column<int>(type: "integer", nullable: false),
                    TagId = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TagRelations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TagRelations_Tags_RelatedTagId",
                        column: x => x.RelatedTagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TagRelations_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TagRelations_RelatedTagId",
                table: "TagRelations",
                column: "RelatedTagId");

            migrationBuilder.CreateIndex(
                name: "IX_TagRelations_TagId_RelatedTagId_Type",
                table: "TagRelations",
                columns: new[] { "TagId", "RelatedTagId", "Type" },
                unique: true);
        }
    }
}
