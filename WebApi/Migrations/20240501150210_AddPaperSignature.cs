using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apachi.WebApi.Migrations
{
    /// <inheritdoc />
    public partial class AddPaperSignature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ClosedDate",
                table: "Submissions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedDate",
                table: "Submissions",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<byte[]>(
                name: "PaperSignature",
                table: "Submissions",
                type: "BLOB",
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClosedDate",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "PaperSignature",
                table: "Submissions");
        }
    }
}
