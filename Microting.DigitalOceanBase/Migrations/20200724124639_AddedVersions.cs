using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Microting.DigitalOceanBase.Migrations
{
    public partial class AddedVersions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DropletTagVersions",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CreatedAt = table.Column<DateTime>(nullable: false),
                    UpdatedAt = table.Column<DateTime>(nullable: true),
                    WorkflowState = table.Column<string>(maxLength: 255, nullable: true),
                    CreatedByUserId = table.Column<int>(nullable: false),
                    UpdatedByUserId = table.Column<int>(nullable: false),
                    Version = table.Column<int>(nullable: false),
                    DropletId = table.Column<int>(nullable: false),
                    TagId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DropletTagVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DropletVersions",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CreatedAt = table.Column<DateTime>(nullable: false),
                    UpdatedAt = table.Column<DateTime>(nullable: true),
                    WorkflowState = table.Column<string>(maxLength: 255, nullable: true),
                    CreatedByUserId = table.Column<int>(nullable: false),
                    UpdatedByUserId = table.Column<int>(nullable: false),
                    Version = table.Column<int>(nullable: false),
                    DoUid = table.Column<int>(nullable: false),
                    CustomerNo = table.Column<int>(nullable: false),
                    PublicIpV4 = table.Column<string>(nullable: true),
                    PrivateIpV4 = table.Column<string>(nullable: true),
                    PublicIpV6 = table.Column<string>(nullable: true),
                    CurrentImageName = table.Column<string>(nullable: true),
                    RequestedImageName = table.Column<string>(nullable: true),
                    CurrentImageId = table.Column<int>(nullable: false),
                    RequestedImageId = table.Column<int>(nullable: false),
                    UserData = table.Column<string>(nullable: true),
                    MonitoringEnabled = table.Column<bool>(nullable: false),
                    IpV6Enabled = table.Column<bool>(nullable: false),
                    BackupsEnabled = table.Column<bool>(nullable: false),
                    DropletTagId = table.Column<int>(nullable: false),
                    Sizeid = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DropletVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DropletVersions_Sizes_Sizeid",
                        column: x => x.Sizeid,
                        principalTable: "Sizes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImageVersions",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CreatedAt = table.Column<DateTime>(nullable: false),
                    UpdatedAt = table.Column<DateTime>(nullable: true),
                    WorkflowState = table.Column<string>(maxLength: 255, nullable: true),
                    CreatedByUserId = table.Column<int>(nullable: false),
                    UpdatedByUserId = table.Column<int>(nullable: false),
                    Version = table.Column<int>(nullable: false),
                    DoUid = table.Column<int>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    Type = table.Column<string>(nullable: true),
                    Distribution = table.Column<string>(nullable: true),
                    Slug = table.Column<string>(nullable: true),
                    Public = table.Column<bool>(nullable: false),
                    ImageCreatedAt = table.Column<DateTime>(nullable: false),
                    MinDiskSize = table.Column<int>(nullable: false),
                    SizeGigabytes = table.Column<double>(nullable: false),
                    Description = table.Column<string>(nullable: true),
                    Status = table.Column<string>(nullable: true),
                    ErrorMessage = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SizeRegionVersions",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
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
                    table.PrimaryKey("PK_SizeRegionVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SizeVersions",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CreatedAt = table.Column<DateTime>(nullable: false),
                    UpdatedAt = table.Column<DateTime>(nullable: true),
                    WorkflowState = table.Column<string>(maxLength: 255, nullable: true),
                    CreatedByUserId = table.Column<int>(nullable: false),
                    UpdatedByUserId = table.Column<int>(nullable: false),
                    Version = table.Column<int>(nullable: false),
                    DropletId = table.Column<int>(nullable: true),
                    SizeRegionId = table.Column<int>(nullable: false),
                    Slug = table.Column<string>(nullable: true),
                    Transfer = table.Column<double>(nullable: false),
                    PriceMonthly = table.Column<decimal>(nullable: false),
                    PriceHourly = table.Column<decimal>(nullable: false),
                    Memory = table.Column<int>(nullable: false),
                    Vcpus = table.Column<int>(nullable: false),
                    Disk = table.Column<int>(nullable: false),
                    Available = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SizeVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SizeVersions_Droplets_DropletId",
                        column: x => x.DropletId,
                        principalTable: "Droplets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DropletVersions_Sizeid",
                table: "DropletVersions",
                column: "Sizeid");

            migrationBuilder.CreateIndex(
                name: "IX_SizeVersions_DropletId",
                table: "SizeVersions",
                column: "DropletId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DropletTagVersions");

            migrationBuilder.DropTable(
                name: "DropletVersions");

            migrationBuilder.DropTable(
                name: "ImageVersions");

            migrationBuilder.DropTable(
                name: "SizeRegionVersions");

            migrationBuilder.DropTable(
                name: "SizeVersions");
        }
    }
}
