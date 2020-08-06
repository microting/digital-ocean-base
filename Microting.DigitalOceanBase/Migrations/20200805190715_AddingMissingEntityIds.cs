using Microsoft.EntityFrameworkCore.Migrations;

namespace Microting.DigitalOceanBase.Migrations
{
    public partial class AddingMissingEntityIds : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SizeId",
                table: "SizeVersions",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SizeRegionId",
                table: "SizeRegionVersions",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ImageId",
                table: "ImageVersions",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DropletId",
                table: "DropletVersions",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DropletTagId",
                table: "DropletTagVersions",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SizeId",
                table: "SizeVersions");

            migrationBuilder.DropColumn(
                name: "SizeRegionId",
                table: "SizeRegionVersions");

            migrationBuilder.DropColumn(
                name: "ImageId",
                table: "ImageVersions");

            migrationBuilder.DropColumn(
                name: "DropletId",
                table: "DropletVersions");

            migrationBuilder.DropColumn(
                name: "DropletTagId",
                table: "DropletTagVersions");
        }
    }
}
