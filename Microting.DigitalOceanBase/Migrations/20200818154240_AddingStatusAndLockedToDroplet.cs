using Microsoft.EntityFrameworkCore.Migrations;

namespace Microting.DigitalOceanBase.Migrations
{
    public partial class AddingStatusAndLockedToDroplet : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Locked",
                table: "DropletVersions",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "DropletVersions",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Locked",
                table: "Droplets",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Droplets",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Locked",
                table: "DropletVersions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "DropletVersions");

            migrationBuilder.DropColumn(
                name: "Locked",
                table: "Droplets");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Droplets");
        }
    }
}
