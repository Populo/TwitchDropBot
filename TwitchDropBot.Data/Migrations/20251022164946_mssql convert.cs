using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TwitchDropBot.Data.Migrations
{
    /// <inheritdoc />
    public partial class mssqlconvert : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Drops",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    GameId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CampaignName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Drops", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Drops");
        }
    }
}
