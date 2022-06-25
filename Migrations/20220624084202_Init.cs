using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeatherProject.Migrations
{
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Weathers",
                columns: table => new
                {
                    WeatherId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Time = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WindDirection = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Visibility = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Temperature = table.Column<int>(type: "int", nullable: false),
                    WeatherCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WindSpeed = table.Column<int>(type: "int", nullable: false),
                    CloudNumber = table.Column<int>(type: "int", nullable: false),
                    CloudBottomLine = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Weathers", x => x.WeatherId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Weathers");
        }
    }
}
