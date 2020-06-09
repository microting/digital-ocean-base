using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.Data.EntityFrameworkCore.Metadata;

namespace Microting.DigitalOceanBase.Migrations
{
    public partial class fixed_regions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Regions_Sizes_SizeId",
                table: "Regions");

            migrationBuilder.DropIndex(
                name: "IX_Regions_SizeId",
                table: "Regions");

            migrationBuilder.DropColumn(
                name: "SizeId",
                table: "Regions");

            migrationBuilder.CreateTable(
                name: "SizeRegion",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    CreatedAt = table.Column<DateTime>(nullable: false),
                    UpdatedAt = table.Column<DateTime>(nullable: true),
                    WorkflowState = table.Column<string>(maxLength: 255, nullable: true),
                    CreatedByUserId = table.Column<int>(nullable: false),
                    UpdatedByUserId = table.Column<int>(nullable: false),
                    Version = table.Column<int>(nullable: false),
                    RegionId = table.Column<int>(nullable: false),
                    SizeId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SizeRegion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SizeRegion_Regions_RegionId",
                        column: x => x.RegionId,
                        principalTable: "Regions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SizeRegion_Sizes_SizeId",
                        column: x => x.SizeId,
                        principalTable: "Sizes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SizeRegion_RegionId",
                table: "SizeRegion",
                column: "RegionId");

            migrationBuilder.CreateIndex(
                name: "IX_SizeRegion_SizeId",
                table: "SizeRegion",
                column: "SizeId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SizeRegion");

            migrationBuilder.AddColumn<int>(
                name: "SizeId",
                table: "Regions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Regions_SizeId",
                table: "Regions",
                column: "SizeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Regions_Sizes_SizeId",
                table: "Regions",
                column: "SizeId",
                principalTable: "Sizes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
