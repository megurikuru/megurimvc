using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Meguri.Migrations
{
    /// <inheritdoc />
    public partial class SyncPendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DocTags_Tags_TagId",
                table: "DocTags");

            migrationBuilder.DropForeignKey(
                name: "FK_Tags_Tags_ParentTagId",
                table: "Tags");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Tags",
                table: "Tags");

            migrationBuilder.DropIndex(
                name: "IX_Tags_Name_ParentTagId",
                table: "Tags");

            migrationBuilder.DropIndex(
                name: "IX_Tags_ParentTagId",
                table: "Tags");

            migrationBuilder.DropIndex(
                name: "IX_Docs_ParentId",
                table: "Docs");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "ParentTagId",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "Docs");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Tags",
                newName: "NormalizedName");

            migrationBuilder.AddColumn<long>(
                name: "TagId",
                table: "Tags",
                type: "bigint",
                nullable: false,
                defaultValue: 0L)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "Tags",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<bool>(
                name: "IsAmbiguous",
                table: "Tags",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<long>(
                name: "TagId",
                table: "DocTags",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<long>(
                name: "BoundTagBoundId",
                table: "DocTags",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CommentCount",
                table: "Docs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsLocked",
                table: "Docs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPinned",
                table: "Docs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastCommentedAt",
                table: "Docs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ViewCount",
                table: "Docs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ActiveAvatarImageId",
                table: "AspNetUsers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Bio",
                table: "AspNetUsers",
                type: "text",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Tags",
                table: "Tags",
                column: "TagId");

            migrationBuilder.CreateTable(
                name: "BoundTag",
                columns: table => new
                {
                    BoundId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MainTagId = table.Column<long>(type: "bigint", nullable: false),
                    ContextTagId = table.Column<long>(type: "bigint", nullable: true),
                    PostCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoundTag", x => x.BoundId);
                    table.ForeignKey(
                        name: "FK_BoundTag_Tags_ContextTagId",
                        column: x => x.ContextTagId,
                        principalTable: "Tags",
                        principalColumn: "TagId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BoundTag_Tags_MainTagId",
                        column: x => x.MainTagId,
                        principalTable: "Tags",
                        principalColumn: "TagId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Comments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: true),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DocId = table.Column<int>(type: "integer", nullable: true),
                    ImageId = table.Column<int>(type: "integer", nullable: true),
                    ParentId = table.Column<int>(type: "integer", nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Comments_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Comments_Comments_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Comments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Comments_Docs_DocId",
                        column: x => x.DocId,
                        principalTable: "Docs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Comments_Images_ImageId",
                        column: x => x.ImageId,
                        principalTable: "Images",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Conversations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "text", nullable: true),
                    IsGroup = table.Column<bool>(type: "boolean", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conversations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImageTags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ImageId = table.Column<int>(type: "integer", nullable: false),
                    TagId = table.Column<long>(type: "bigint", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImageTags_Images_ImageId",
                        column: x => x.ImageId,
                        principalTable: "Images",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ImageTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "TagId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: true),
                    ImageId = table.Column<int>(type: "integer", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserImages_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserImages_Images_ImageId",
                        column: x => x.ImageId,
                        principalTable: "Images",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommentImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CommentId = table.Column<int>(type: "integer", nullable: false),
                    ImageId = table.Column<int>(type: "integer", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommentImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommentImages_Comments_CommentId",
                        column: x => x.CommentId,
                        principalTable: "Comments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CommentImages_Images_ImageId",
                        column: x => x.ImageId,
                        principalTable: "Images",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConversationMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConversationId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: true),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversationMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConversationMembers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConversationMembers_Conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "Conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConversationId = table.Column<int>(type: "integer", nullable: false),
                    SenderId = table.Column<string>(type: "text", nullable: true),
                    Text = table.Column<string>(type: "text", nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Messages_AspNetUsers_SenderId",
                        column: x => x.SenderId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Messages_Conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "Conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MessageImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MessageId = table.Column<int>(type: "integer", nullable: false),
                    ImageId = table.Column<int>(type: "integer", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessageImages_Images_ImageId",
                        column: x => x.ImageId,
                        principalTable: "Images",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MessageImages_Messages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "Messages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    DocId = table.Column<int>(type: "integer", nullable: true),
                    CommentId = table.Column<int>(type: "integer", nullable: true),
                    ImageId = table.Column<int>(type: "integer", nullable: true),
                    MessageId = table.Column<int>(type: "integer", nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reactions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reactions_Comments_CommentId",
                        column: x => x.CommentId,
                        principalTable: "Comments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reactions_Docs_DocId",
                        column: x => x.DocId,
                        principalTable: "Docs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reactions_Images_ImageId",
                        column: x => x.ImageId,
                        principalTable: "Images",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reactions_Messages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "Messages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Fandoms",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "イラスト" },
                    { 2, "漫画" },
                    { 3, "小説" },
                    { 4, "車" },
                    { 5, "プログラミング" },
                    { 6, "ゲーム" },
                    { 7, "コスプレ" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tags_NormalizedName",
                table: "Tags",
                column: "NormalizedName");

            migrationBuilder.CreateIndex(
                name: "IX_DocTags_BoundTagBoundId",
                table: "DocTags",
                column: "BoundTagBoundId");

            migrationBuilder.CreateIndex(
                name: "IX_Docs_IsPinned",
                table: "Docs",
                column: "IsPinned");

            migrationBuilder.CreateIndex(
                name: "IX_Docs_LastCommentedAt",
                table: "Docs",
                column: "LastCommentedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Docs_UserId",
                table: "Docs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_ActiveAvatarImageId",
                table: "AspNetUsers",
                column: "ActiveAvatarImageId");

            migrationBuilder.CreateIndex(
                name: "IX_BoundTag_ContextTagId",
                table: "BoundTag",
                column: "ContextTagId");

            migrationBuilder.CreateIndex(
                name: "IX_BoundTag_MainTagId_ContextTagId",
                table: "BoundTag",
                columns: new[] { "MainTagId", "ContextTagId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommentImages_CommentId_ImageId",
                table: "CommentImages",
                columns: new[] { "CommentId", "ImageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommentImages_ImageId",
                table: "CommentImages",
                column: "ImageId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_Created",
                table: "Comments",
                column: "Created");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_DocId",
                table: "Comments",
                column: "DocId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_DocId_Number",
                table: "Comments",
                columns: new[] { "DocId", "Number" });

            migrationBuilder.CreateIndex(
                name: "IX_Comments_ImageId",
                table: "Comments",
                column: "ImageId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_ParentId",
                table: "Comments",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_UserId",
                table: "Comments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationMembers_ConversationId_UserId",
                table: "ConversationMembers",
                columns: new[] { "ConversationId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConversationMembers_UserId",
                table: "ConversationMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_Created",
                table: "Conversations",
                column: "Created");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_Updated",
                table: "Conversations",
                column: "Updated");

            migrationBuilder.CreateIndex(
                name: "IX_ImageTags_ImageId_TagId",
                table: "ImageTags",
                columns: new[] { "ImageId", "TagId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImageTags_TagId",
                table: "ImageTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageImages_ImageId",
                table: "MessageImages",
                column: "ImageId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageImages_MessageId_ImageId",
                table: "MessageImages",
                columns: new[] { "MessageId", "ImageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ConversationId",
                table: "Messages",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_Created",
                table: "Messages",
                column: "Created");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_SenderId",
                table: "Messages",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_Reactions_CommentId",
                table: "Reactions",
                column: "CommentId");

            migrationBuilder.CreateIndex(
                name: "IX_Reactions_Created",
                table: "Reactions",
                column: "Created");

            migrationBuilder.CreateIndex(
                name: "IX_Reactions_DocId",
                table: "Reactions",
                column: "DocId");

            migrationBuilder.CreateIndex(
                name: "IX_Reactions_ImageId",
                table: "Reactions",
                column: "ImageId");

            migrationBuilder.CreateIndex(
                name: "IX_Reactions_MessageId",
                table: "Reactions",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_Reactions_Type",
                table: "Reactions",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Reactions_UserId",
                table: "Reactions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserImages_ImageId",
                table: "UserImages",
                column: "ImageId");

            migrationBuilder.CreateIndex(
                name: "IX_UserImages_UserId",
                table: "UserImages",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserImages_UserId_ImageId",
                table: "UserImages",
                columns: new[] { "UserId", "ImageId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Images_ActiveAvatarImageId",
                table: "AspNetUsers",
                column: "ActiveAvatarImageId",
                principalTable: "Images",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Docs_AspNetUsers_UserId",
                table: "Docs",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DocTags_BoundTag_BoundTagBoundId",
                table: "DocTags",
                column: "BoundTagBoundId",
                principalTable: "BoundTag",
                principalColumn: "BoundId");

            migrationBuilder.AddForeignKey(
                name: "FK_DocTags_Tags_TagId",
                table: "DocTags",
                column: "TagId",
                principalTable: "Tags",
                principalColumn: "TagId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Images_ActiveAvatarImageId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_Docs_AspNetUsers_UserId",
                table: "Docs");

            migrationBuilder.DropForeignKey(
                name: "FK_DocTags_BoundTag_BoundTagBoundId",
                table: "DocTags");

            migrationBuilder.DropForeignKey(
                name: "FK_DocTags_Tags_TagId",
                table: "DocTags");

            migrationBuilder.DropTable(
                name: "BoundTag");

            migrationBuilder.DropTable(
                name: "CommentImages");

            migrationBuilder.DropTable(
                name: "ConversationMembers");

            migrationBuilder.DropTable(
                name: "ImageTags");

            migrationBuilder.DropTable(
                name: "MessageImages");

            migrationBuilder.DropTable(
                name: "Reactions");

            migrationBuilder.DropTable(
                name: "UserImages");

            migrationBuilder.DropTable(
                name: "Comments");

            migrationBuilder.DropTable(
                name: "Messages");

            migrationBuilder.DropTable(
                name: "Conversations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Tags",
                table: "Tags");

            migrationBuilder.DropIndex(
                name: "IX_Tags_NormalizedName",
                table: "Tags");

            migrationBuilder.DropIndex(
                name: "IX_DocTags_BoundTagBoundId",
                table: "DocTags");

            migrationBuilder.DropIndex(
                name: "IX_Docs_IsPinned",
                table: "Docs");

            migrationBuilder.DropIndex(
                name: "IX_Docs_LastCommentedAt",
                table: "Docs");

            migrationBuilder.DropIndex(
                name: "IX_Docs_UserId",
                table: "Docs");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_ActiveAvatarImageId",
                table: "AspNetUsers");

            migrationBuilder.DeleteData(
                table: "Fandoms",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Fandoms",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Fandoms",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Fandoms",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Fandoms",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Fandoms",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Fandoms",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DropColumn(
                name: "TagId",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "IsAmbiguous",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "BoundTagBoundId",
                table: "DocTags");

            migrationBuilder.DropColumn(
                name: "CommentCount",
                table: "Docs");

            migrationBuilder.DropColumn(
                name: "IsLocked",
                table: "Docs");

            migrationBuilder.DropColumn(
                name: "IsPinned",
                table: "Docs");

            migrationBuilder.DropColumn(
                name: "LastCommentedAt",
                table: "Docs");

            migrationBuilder.DropColumn(
                name: "ViewCount",
                table: "Docs");

            migrationBuilder.DropColumn(
                name: "ActiveAvatarImageId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Bio",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "NormalizedName",
                table: "Tags",
                newName: "Description");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "Tags",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<int>(
                name: "ParentTagId",
                table: "Tags",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "TagId",
                table: "DocTags",
                type: "integer",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<int>(
                name: "ParentId",
                table: "Docs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Tags",
                table: "Tags",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_Name_ParentTagId",
                table: "Tags",
                columns: new[] { "Name", "ParentTagId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tags_ParentTagId",
                table: "Tags",
                column: "ParentTagId");

            migrationBuilder.CreateIndex(
                name: "IX_Docs_ParentId",
                table: "Docs",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_DocTags_Tags_TagId",
                table: "DocTags",
                column: "TagId",
                principalTable: "Tags",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Tags_Tags_ParentTagId",
                table: "Tags",
                column: "ParentTagId",
                principalTable: "Tags",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
