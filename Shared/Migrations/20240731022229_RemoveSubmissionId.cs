using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apachi.Shared.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSubmissionId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubmissionId",
                table: "Entries");

            migrationBuilder.RenameColumn(
                name: "MessageBytes",
                table: "Entries",
                newName: "Data");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Data",
                table: "Entries",
                newName: "MessageBytes");

            migrationBuilder.AddColumn<Guid>(
                name: "SubmissionId",
                table: "Entries",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }
    }
}
