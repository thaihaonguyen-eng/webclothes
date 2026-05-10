using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace webclothes.Migrations
{
    public partial class Phase2_Optimization : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Orders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Categories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Categories",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "IsDeleted", table: "Products");
            migrationBuilder.DropColumn(name: "Slug", table: "Products");
            migrationBuilder.DropColumn(name: "IsDeleted", table: "Orders");
            migrationBuilder.DropColumn(name: "IsDeleted", table: "Categories");
            migrationBuilder.DropColumn(name: "Slug", table: "Categories");
        }
    }
}
