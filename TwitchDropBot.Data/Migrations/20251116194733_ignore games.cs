using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TwitchDropBot.Data.Migrations
{
    /// <inheritdoc />
    public partial class ignoregames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IgnoredGames",
                columns: table => new
                {
                    Game = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IgnoredGames", x => x.Game);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IgnoredGames");
        }
    }
}
