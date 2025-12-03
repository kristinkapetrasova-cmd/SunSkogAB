using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SunSkog.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class TeamMembership_AddRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsLead",
                table: "TeamMemberships");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "FromDate",
                table: "TeamMemberships",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "TeamMemberships",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Member");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Role",
                table: "TeamMemberships");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "FromDate",
                table: "TeamMemberships",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1),
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsLead",
                table: "TeamMemberships",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
