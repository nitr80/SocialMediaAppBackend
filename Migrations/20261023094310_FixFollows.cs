using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocialMediaAppBackend.Migrations
{
    /// <inheritdoc />
    public partial class FixFollows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Follows_Users_UserId",
                table: "Follows");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Follows",
                newName: "FollowerId");

            migrationBuilder.RenameIndex(
                name: "IX_Follows_UserId_FollowedUserId",
                table: "Follows",
                newName: "IX_Follows_FollowerId_FollowedUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Follows_Users_FollowerId",
                table: "Follows",
                column: "FollowerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Follows_Users_FollowerId",
                table: "Follows");

            migrationBuilder.RenameColumn(
                name: "FollowerId",
                table: "Follows",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Follows_FollowerId_FollowedUserId",
                table: "Follows",
                newName: "IX_Follows_UserId_FollowedUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Follows_Users_UserId",
                table: "Follows",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
