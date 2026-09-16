using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Meguri.Migrations
{
    /// <inheritdoc />
    public partial class AddRecommendedIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tags_NormalizedName",
                table: "Tags");

            migrationBuilder.DropIndex(
                name: "IX_Comments_DocId_Number",
                table: "Comments");

            migrationBuilder.AddColumn<int>(
                name: "ParentFandomId",
                table: "Fandoms",
                type: "integer",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Fandoms",
                keyColumn: "Id",
                keyValue: 1,
                column: "ParentFandomId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Fandoms",
                keyColumn: "Id",
                keyValue: 2,
                column: "ParentFandomId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Fandoms",
                keyColumn: "Id",
                keyValue: 3,
                column: "ParentFandomId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Fandoms",
                keyColumn: "Id",
                keyValue: 4,
                column: "ParentFandomId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Fandoms",
                keyColumn: "Id",
                keyValue: 5,
                column: "ParentFandomId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Fandoms",
                keyColumn: "Id",
                keyValue: 6,
                column: "ParentFandomId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Fandoms",
                keyColumn: "Id",
                keyValue: 7,
                column: "ParentFandomId",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_Tags_Name",
                table: "Tags",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_NormalizedName",
                table: "Tags",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reactions_CommentId_Type",
                table: "Reactions",
                columns: new[] { "CommentId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_Reactions_DocId_Type",
                table: "Reactions",
                columns: new[] { "DocId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_Reactions_UserId_CommentId_Type",
                table: "Reactions",
                columns: new[] { "UserId", "CommentId", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reactions_UserId_DocId_Type",
                table: "Reactions",
                columns: new[] { "UserId", "DocId", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reactions_UserId_ImageId_Type",
                table: "Reactions",
                columns: new[] { "UserId", "ImageId", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reactions_UserId_MessageId_Type",
                table: "Reactions",
                columns: new[] { "UserId", "MessageId", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ConversationId_Created",
                table: "Messages",
                columns: new[] { "ConversationId", "Created" });

            migrationBuilder.CreateIndex(
                name: "IX_Images_Created",
                table: "Images",
                column: "Created");

            migrationBuilder.CreateIndex(
                name: "IX_Images_IsPublic",
                table: "Images",
                column: "IsPublic");

            migrationBuilder.CreateIndex(
                name: "IX_Images_IsPublic_Created",
                table: "Images",
                columns: new[] { "IsPublic", "Created" });

            migrationBuilder.CreateIndex(
                name: "IX_Fandoms_Name",
                table: "Fandoms",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Fandoms_ParentFandomId",
                table: "Fandoms",
                column: "ParentFandomId");

            migrationBuilder.CreateIndex(
                name: "IX_Docs_FandomId_Created",
                table: "Docs",
                columns: new[] { "FandomId", "Created" });

            migrationBuilder.CreateIndex(
                name: "IX_Docs_FandomId_LastCommentedAt",
                table: "Docs",
                columns: new[] { "FandomId", "LastCommentedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Docs_IsPublic",
                table: "Docs",
                column: "IsPublic");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_DocId_Created",
                table: "Comments",
                columns: new[] { "DocId", "Created" });

            migrationBuilder.CreateIndex(
                name: "IX_Comments_DocId_Number",
                table: "Comments",
                columns: new[] { "DocId", "Number" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Fandoms_Fandoms_ParentFandomId",
                table: "Fandoms",
                column: "ParentFandomId",
                principalTable: "Fandoms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Fandoms_Fandoms_ParentFandomId",
                table: "Fandoms");

            migrationBuilder.DropIndex(
                name: "IX_Tags_Name",
                table: "Tags");

            migrationBuilder.DropIndex(
                name: "IX_Tags_NormalizedName",
                table: "Tags");

            migrationBuilder.DropIndex(
                name: "IX_Reactions_CommentId_Type",
                table: "Reactions");

            migrationBuilder.DropIndex(
                name: "IX_Reactions_DocId_Type",
                table: "Reactions");

            migrationBuilder.DropIndex(
                name: "IX_Reactions_UserId_CommentId_Type",
                table: "Reactions");

            migrationBuilder.DropIndex(
                name: "IX_Reactions_UserId_DocId_Type",
                table: "Reactions");

            migrationBuilder.DropIndex(
                name: "IX_Reactions_UserId_ImageId_Type",
                table: "Reactions");

            migrationBuilder.DropIndex(
                name: "IX_Reactions_UserId_MessageId_Type",
                table: "Reactions");

            migrationBuilder.DropIndex(
                name: "IX_Messages_ConversationId_Created",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Images_Created",
                table: "Images");

            migrationBuilder.DropIndex(
                name: "IX_Images_IsPublic",
                table: "Images");

            migrationBuilder.DropIndex(
                name: "IX_Images_IsPublic_Created",
                table: "Images");

            migrationBuilder.DropIndex(
                name: "IX_Fandoms_Name",
                table: "Fandoms");

            migrationBuilder.DropIndex(
                name: "IX_Fandoms_ParentFandomId",
                table: "Fandoms");

            migrationBuilder.DropIndex(
                name: "IX_Docs_FandomId_Created",
                table: "Docs");

            migrationBuilder.DropIndex(
                name: "IX_Docs_FandomId_LastCommentedAt",
                table: "Docs");

            migrationBuilder.DropIndex(
                name: "IX_Docs_IsPublic",
                table: "Docs");

            migrationBuilder.DropIndex(
                name: "IX_Comments_DocId_Created",
                table: "Comments");

            migrationBuilder.DropIndex(
                name: "IX_Comments_DocId_Number",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "ParentFandomId",
                table: "Fandoms");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_NormalizedName",
                table: "Tags",
                column: "NormalizedName");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_DocId_Number",
                table: "Comments",
                columns: new[] { "DocId", "Number" });
        }
    }
}
