using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZhihuClone.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPrivacyAndNotificationSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowDirectMessage",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllowFollow",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllowNotification",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CommentNotification",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EmailNotification",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "FollowNotification",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "LikeNotification",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PushNotification",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowCompany",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowEmail",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowLocation",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowPhone",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SystemNotification",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Collections",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ViewCount",
                table: "Collections",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowDirectMessage",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AllowFollow",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AllowNotification",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CommentNotification",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmailNotification",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "FollowNotification",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LikeNotification",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PushNotification",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ShowCompany",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ShowEmail",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ShowLocation",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ShowPhone",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SystemNotification",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Collections");

            migrationBuilder.DropColumn(
                name: "ViewCount",
                table: "Collections");
        }
    }
}
