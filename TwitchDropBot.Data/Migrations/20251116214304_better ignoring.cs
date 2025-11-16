using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TwitchDropBot.Data.Migrations
{
    /// <inheritdoc />
    public partial class betterignoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IgnoredGames");

            migrationBuilder.AlterColumn<string>(
                name: "GameId",
                table: "Drops",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateTable(
                name: "Games",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Ignored = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Games", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Drops_GameId",
                table: "Drops",
                column: "GameId");

            migrationBuilder.AddForeignKey(
                name: "FK_Drops_Games_GameId",
                table: "Drops",
                column: "GameId",
                principalTable: "Games",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Drops_Games_GameId",
                table: "Drops");

            migrationBuilder.DropTable(
                name: "Games");

            migrationBuilder.DropIndex(
                name: "IX_Drops_GameId",
                table: "Drops");

            migrationBuilder.AlterColumn<string>(
                name: "GameId",
                table: "Drops",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

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
    }
}
