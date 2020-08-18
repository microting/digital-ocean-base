using Microsoft.EntityFrameworkCore.Migrations;

namespace Microting.DigitalOceanBase.Migrations
{
    public partial class FixingSizeRelationShip : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SizeVersions_Droplets_DropletId",
                table: "SizeVersions");

            migrationBuilder.DropIndex(
                name: "IX_SizeVersions_DropletId",
                table: "SizeVersions");

            migrationBuilder.DropColumn(
                name: "SizeRegionId",
                table: "SizeVersions");

            migrationBuilder.AlterColumn<int>(
                name: "DropletId",
                table: "SizeVersions",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "DropletId",
                table: "SizeVersions",
                type: "int",
                nullable: true,
                oldClrType: typeof(int));

            migrationBuilder.AddColumn<int>(
                name: "SizeRegionId",
                table: "SizeVersions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_SizeVersions_DropletId",
                table: "SizeVersions",
                column: "DropletId");

            migrationBuilder.AddForeignKey(
                name: "FK_SizeVersions_Droplets_DropletId",
                table: "SizeVersions",
                column: "DropletId",
                principalTable: "Droplets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
