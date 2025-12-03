using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SunSkog.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class TeamMembership_AddDateRange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_InventoryItems_ItemId",
                table: "Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_TeamMemberships_Teams_TeamId",
                table: "TeamMemberships");

            migrationBuilder.DropIndex(
                name: "IX_TimesheetEntries_TimesheetId",
                table: "TimesheetEntries");

            migrationBuilder.DropIndex(
                name: "IX_TeamMemberships_TeamId_UserId_LeftAt",
                table: "TeamMemberships");

            migrationBuilder.DropColumn(
                name: "JoinedAt",
                table: "TeamMemberships");

            migrationBuilder.DropColumn(
                name: "LeftAt",
                table: "TeamMemberships");

            migrationBuilder.AlterColumn<string>(
                name: "EmployeeId",
                table: "Timesheets",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Teams",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<DateOnly>(
                name: "FromDate",
                table: "TeamMemberships",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<bool>(
                name: "IsLead",
                table: "TeamMemberships",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "TeamId1",
                table: "TeamMemberships",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ToDate",
                table: "TeamMemberships",
                type: "date",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SerialNumber",
                table: "InventoryItems",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SKU",
                table: "InventoryItems",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "InventoryItems",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.CreateTable(
                name: "Rates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HourRate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    KmRate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PieceRate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "date", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Timesheets_EmployeeId_PeriodStart_PeriodEnd",
                table: "Timesheets",
                columns: new[] { "EmployeeId", "PeriodStart", "PeriodEnd" });

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetEntries_TimesheetId_WorkDate",
                table: "TimesheetEntries",
                columns: new[] { "TimesheetId", "WorkDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Teams_Name",
                table: "Teams",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMemberships_TeamId_UserId_FromDate",
                table: "TeamMemberships",
                columns: new[] { "TeamId", "UserId", "FromDate" });

            migrationBuilder.CreateIndex(
                name: "IX_TeamMemberships_TeamId1",
                table: "TeamMemberships",
                column: "TeamId1");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_Name",
                table: "InventoryItems",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_SerialNumber",
                table: "InventoryItems",
                column: "SerialNumber");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_SKU",
                table: "InventoryItems",
                column: "SKU");

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalLogs_Timesheets_TimesheetId",
                table: "ApprovalLogs",
                column: "TimesheetId",
                principalTable: "Timesheets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_InventoryItems_ItemId",
                table: "Assignments",
                column: "ItemId",
                principalTable: "InventoryItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TeamMemberships_Teams_TeamId",
                table: "TeamMemberships",
                column: "TeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TeamMemberships_Teams_TeamId1",
                table: "TeamMemberships",
                column: "TeamId1",
                principalTable: "Teams",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApprovalLogs_Timesheets_TimesheetId",
                table: "ApprovalLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_InventoryItems_ItemId",
                table: "Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_TeamMemberships_Teams_TeamId",
                table: "TeamMemberships");

            migrationBuilder.DropForeignKey(
                name: "FK_TeamMemberships_Teams_TeamId1",
                table: "TeamMemberships");

            migrationBuilder.DropTable(
                name: "Rates");

            migrationBuilder.DropIndex(
                name: "IX_Timesheets_EmployeeId_PeriodStart_PeriodEnd",
                table: "Timesheets");

            migrationBuilder.DropIndex(
                name: "IX_TimesheetEntries_TimesheetId_WorkDate",
                table: "TimesheetEntries");

            migrationBuilder.DropIndex(
                name: "IX_Teams_Name",
                table: "Teams");

            migrationBuilder.DropIndex(
                name: "IX_TeamMemberships_TeamId_UserId_FromDate",
                table: "TeamMemberships");

            migrationBuilder.DropIndex(
                name: "IX_TeamMemberships_TeamId1",
                table: "TeamMemberships");

            migrationBuilder.DropIndex(
                name: "IX_InventoryItems_Name",
                table: "InventoryItems");

            migrationBuilder.DropIndex(
                name: "IX_InventoryItems_SerialNumber",
                table: "InventoryItems");

            migrationBuilder.DropIndex(
                name: "IX_InventoryItems_SKU",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "FromDate",
                table: "TeamMemberships");

            migrationBuilder.DropColumn(
                name: "IsLead",
                table: "TeamMemberships");

            migrationBuilder.DropColumn(
                name: "TeamId1",
                table: "TeamMemberships");

            migrationBuilder.DropColumn(
                name: "ToDate",
                table: "TeamMemberships");

            migrationBuilder.AlterColumn<string>(
                name: "EmployeeId",
                table: "Timesheets",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Teams",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<DateTime>(
                name: "JoinedAt",
                table: "TeamMemberships",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "LeftAt",
                table: "TeamMemberships",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SerialNumber",
                table: "InventoryItems",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SKU",
                table: "InventoryItems",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "InventoryItems",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetEntries_TimesheetId",
                table: "TimesheetEntries",
                column: "TimesheetId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMemberships_TeamId_UserId_LeftAt",
                table: "TeamMemberships",
                columns: new[] { "TeamId", "UserId", "LeftAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_InventoryItems_ItemId",
                table: "Assignments",
                column: "ItemId",
                principalTable: "InventoryItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TeamMemberships_Teams_TeamId",
                table: "TeamMemberships",
                column: "TeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
