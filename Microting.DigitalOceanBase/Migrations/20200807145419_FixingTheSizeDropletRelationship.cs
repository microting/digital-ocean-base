using Microsoft.EntityFrameworkCore.Migrations;

namespace Microting.DigitalOceanBase.Migrations
{
    public partial class FixingTheSizeDropletRelationship : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Droplets_Sizes_Sizeid",
                table: "Droplets");

            migrationBuilder.DropIndex(
                name: "IX_Droplets_Sizeid",
                table: "Droplets");

            migrationBuilder.AddColumn<int>(
                name: "DropletId",
                table: "Sizes",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DropletId",
                table: "Sizes");

            migrationBuilder.CreateIndex(
                name: "IX_Droplets_Sizeid",
                table: "Droplets",
                column: "Sizeid",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Droplets_Sizes_Sizeid",
                table: "Droplets",
                column: "Sizeid",
                principalTable: "Sizes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
