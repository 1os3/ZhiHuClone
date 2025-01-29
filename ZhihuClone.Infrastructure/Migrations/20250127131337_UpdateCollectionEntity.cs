using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZhihuClone.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCollectionEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CollectionFollowers",
                columns: table => new
                {
                    FollowersId = table.Column<int>(type: "int", nullable: false),
                    CollectionUserId = table.Column<int>(type: "int", nullable: false),
                    CollectionPostId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionFollowers", x => new { x.FollowersId, x.CollectionUserId, x.CollectionPostId });
                    table.ForeignKey(
                        name: "FK_CollectionFollowers_Collections_CollectionUserId_CollectionPostId",
                        columns: x => new { x.CollectionUserId, x.CollectionPostId },
                        principalTable: "Collections",
                        principalColumns: new[] { "UserId", "PostId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CollectionFollowers_Users_FollowersId",
                        column: x => x.FollowersId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CollectionPosts",
                columns: table => new
                {
                    PostsId = table.Column<int>(type: "int", nullable: false),
                    CollectionUserId = table.Column<int>(type: "int", nullable: false),
                    CollectionPostId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionPosts", x => new { x.PostsId, x.CollectionUserId, x.CollectionPostId });
                    table.ForeignKey(
                        name: "FK_CollectionPosts_Collections_CollectionUserId_CollectionPostId",
                        columns: x => new { x.CollectionUserId, x.CollectionPostId },
                        principalTable: "Collections",
                        principalColumns: new[] { "UserId", "PostId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CollectionPosts_Posts_PostsId",
                        column: x => x.PostsId,
                        principalTable: "Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CollectionFollowers_CollectionUserId_CollectionPostId",
                table: "CollectionFollowers",
                columns: new[] { "CollectionUserId", "CollectionPostId" });

            migrationBuilder.CreateIndex(
                name: "IX_CollectionPosts_CollectionUserId_CollectionPostId",
                table: "CollectionPosts",
                columns: new[] { "CollectionUserId", "CollectionPostId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CollectionFollowers");

            migrationBuilder.DropTable(
                name: "CollectionPosts");
        }
    }
}
