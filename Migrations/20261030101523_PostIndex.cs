using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocialMediaAppBackend.Migrations
{
    /// <inheritdoc />
    public partial class PostIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Posts_Id_CreatedAt",
                table: "Posts",
                columns: new[] { "Id", "CreatedAt" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Posts_Id_CreatedAt",
                table: "Posts");
        }
    }
}
