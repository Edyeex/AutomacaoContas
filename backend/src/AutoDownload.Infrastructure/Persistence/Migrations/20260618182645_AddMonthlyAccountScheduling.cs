using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoDownload.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMonthlyAccountScheduling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsScheduleEnabled",
                table: "accounts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ScheduleDayOfMonth",
                table: "accounts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "ScheduleTime",
                table: "accounts",
                type: "time without time zone",
                nullable: false,
                defaultValue: new TimeOnly(9, 0, 0));

            migrationBuilder.Sql("UPDATE accounts SET \"NextRunAt\" = NULL;");

            migrationBuilder.CreateIndex(
                name: "IX_accounts_IsScheduleEnabled_NextRunAt",
                table: "accounts",
                columns: new[] { "IsScheduleEnabled", "NextRunAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_accounts_IsScheduleEnabled_NextRunAt",
                table: "accounts");

            migrationBuilder.DropColumn(
                name: "IsScheduleEnabled",
                table: "accounts");

            migrationBuilder.DropColumn(
                name: "ScheduleDayOfMonth",
                table: "accounts");

            migrationBuilder.DropColumn(
                name: "ScheduleTime",
                table: "accounts");
        }
    }
}
