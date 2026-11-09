using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocialMediaAppBackend.Migrations
{
    /// <inheritdoc />
    public partial class OptimizePostKeysetIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Posts_Id_CreatedAt",
                table: "Posts");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_CreatedAt_Id",
                table: "Posts",
                columns: new[] { "CreatedAt", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Posts_CreatedAt_Id",
                table: "Posts");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_Id_CreatedAt",
                table: "Posts",
                columns: new[] { "Id", "CreatedAt" },
                unique: true);
        }
    }
}
