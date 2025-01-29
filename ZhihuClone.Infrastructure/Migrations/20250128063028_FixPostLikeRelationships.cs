using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZhihuClone.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixPostLikeRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Id",
                table: "PostLikes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "PostLikes",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
