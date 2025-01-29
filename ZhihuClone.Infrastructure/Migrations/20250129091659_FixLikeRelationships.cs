using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZhihuClone.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixLikeRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CommentLikes_Comments_CommentId1",
                table: "CommentLikes");

            migrationBuilder.DropForeignKey(
                name: "FK_CommentLikes_Users_UserId1",
                table: "CommentLikes");

            migrationBuilder.DropForeignKey(
                name: "FK_PostLikes_Posts_PostId1",
                table: "PostLikes");

            migrationBuilder.DropForeignKey(
                name: "FK_PostLikes_Users_UserId1",
                table: "PostLikes");

            migrationBuilder.RenameColumn(
                name: "UserId1",
                table: "PostLikes",
                newName: "UserId2");

            migrationBuilder.RenameColumn(
                name: "PostId1",
                table: "PostLikes",
                newName: "PostId2");

            migrationBuilder.RenameIndex(
                name: "IX_PostLikes_UserId1",
                table: "PostLikes",
                newName: "IX_PostLikes_UserId2");

            migrationBuilder.RenameIndex(
                name: "IX_PostLikes_PostId1",
                table: "PostLikes",
                newName: "IX_PostLikes_PostId2");

            migrationBuilder.RenameColumn(
                name: "UserId1",
                table: "CommentLikes",
                newName: "UserId2");

            migrationBuilder.RenameColumn(
                name: "CommentId1",
                table: "CommentLikes",
                newName: "CommentId2");

            migrationBuilder.RenameIndex(
                name: "IX_CommentLikes_UserId1",
                table: "CommentLikes",
                newName: "IX_CommentLikes_UserId2");

            migrationBuilder.RenameIndex(
                name: "IX_CommentLikes_CommentId1",
                table: "CommentLikes",
                newName: "IX_CommentLikes_CommentId2");

            migrationBuilder.AddForeignKey(
                name: "FK_CommentLikes_Comments_CommentId2",
                table: "CommentLikes",
                column: "CommentId2",
                principalTable: "Comments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CommentLikes_Users_UserId2",
                table: "CommentLikes",
                column: "UserId2",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PostLikes_Posts_PostId2",
                table: "PostLikes",
                column: "PostId2",
                principalTable: "Posts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PostLikes_Users_UserId2",
                table: "PostLikes",
                column: "UserId2",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CommentLikes_Comments_CommentId2",
                table: "CommentLikes");

            migrationBuilder.DropForeignKey(
                name: "FK_CommentLikes_Users_UserId2",
                table: "CommentLikes");

            migrationBuilder.DropForeignKey(
                name: "FK_PostLikes_Posts_PostId2",
                table: "PostLikes");

            migrationBuilder.DropForeignKey(
                name: "FK_PostLikes_Users_UserId2",
                table: "PostLikes");

            migrationBuilder.RenameColumn(
                name: "UserId2",
                table: "PostLikes",
                newName: "UserId1");

            migrationBuilder.RenameColumn(
                name: "PostId2",
                table: "PostLikes",
                newName: "PostId1");

            migrationBuilder.RenameIndex(
                name: "IX_PostLikes_UserId2",
                table: "PostLikes",
                newName: "IX_PostLikes_UserId1");

            migrationBuilder.RenameIndex(
                name: "IX_PostLikes_PostId2",
                table: "PostLikes",
                newName: "IX_PostLikes_PostId1");

            migrationBuilder.RenameColumn(
                name: "UserId2",
                table: "CommentLikes",
                newName: "UserId1");

            migrationBuilder.RenameColumn(
                name: "CommentId2",
                table: "CommentLikes",
                newName: "CommentId1");

            migrationBuilder.RenameIndex(
                name: "IX_CommentLikes_UserId2",
                table: "CommentLikes",
                newName: "IX_CommentLikes_UserId1");

            migrationBuilder.RenameIndex(
                name: "IX_CommentLikes_CommentId2",
                table: "CommentLikes",
                newName: "IX_CommentLikes_CommentId1");

            migrationBuilder.AddForeignKey(
                name: "FK_CommentLikes_Comments_CommentId1",
                table: "CommentLikes",
                column: "CommentId1",
                principalTable: "Comments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CommentLikes_Users_UserId1",
                table: "CommentLikes",
                column: "UserId1",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PostLikes_Posts_PostId1",
                table: "PostLikes",
                column: "PostId1",
                principalTable: "Posts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PostLikes_Users_UserId1",
                table: "PostLikes",
                column: "UserId1",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
