using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apachi.ProgramCommittee.Migrations
{
    /// <inheritdoc />
    public partial class RewriteJobSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Payload",
                table: "Jobs");

            migrationBuilder.RenameColumn(
                name: "Result",
                table: "Jobs",
                newName: "ErrorMessage");

            migrationBuilder.AddColumn<Guid>(
                name: "SubmissionId",
                table: "Jobs",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubmissionId",
                table: "Jobs");

            migrationBuilder.RenameColumn(
                name: "ErrorMessage",
                table: "Jobs",
                newName: "Result");

            migrationBuilder.AddColumn<string>(
                name: "Payload",
                table: "Jobs",
                type: "TEXT",
                nullable: true);
        }
    }
}
