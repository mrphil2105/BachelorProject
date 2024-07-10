using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apachi.WebApi.Migrations
{
    /// <inheritdoc />
    public partial class SwitchToIntervalJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "Jobs");

            migrationBuilder.RenameColumn(
                name: "StartDate",
                table: "Jobs",
                newName: "CompletedDate");

            migrationBuilder.RenameColumn(
                name: "ScheduleDate",
                table: "Jobs",
                newName: "CreatedDate");

            migrationBuilder.CreateTable(
                name: "JobSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    JobType = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    LastRun = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Interval = table.Column<TimeSpan>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobSchedules", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobSchedules_JobType",
                table: "JobSchedules",
                column: "JobType",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobSchedules");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "Jobs",
                newName: "ScheduleDate");

            migrationBuilder.RenameColumn(
                name: "CompletedDate",
                table: "Jobs",
                newName: "StartDate");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EndDate",
                table: "Jobs",
                type: "TEXT",
                nullable: true);
        }
    }
}
