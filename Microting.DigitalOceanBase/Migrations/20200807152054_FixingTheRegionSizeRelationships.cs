using Microsoft.EntityFrameworkCore.Migrations;

namespace Microting.DigitalOceanBase.Migrations
{
    public partial class FixingTheRegionSizeRelationships : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SizeRegion_Regions_RegionId",
                table: "SizeRegion");

            migrationBuilder.DropIndex(
                name: "IX_SizeRegion_RegionId",
                table: "SizeRegion");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_SizeRegion_RegionId",
                table: "SizeRegion",
                column: "RegionId");

            migrationBuilder.AddForeignKey(
                name: "FK_SizeRegion_Regions_RegionId",
                table: "SizeRegion",
                column: "RegionId",
                principalTable: "Regions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
