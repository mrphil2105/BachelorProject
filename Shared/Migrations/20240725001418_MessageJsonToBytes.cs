using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apachi.Shared.Migrations
{
    /// <inheritdoc />
    public partial class MessageJsonToBytes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MessageJson",
                table: "Entries");

            migrationBuilder.AddColumn<byte[]>(
                name: "MessageBytes",
                table: "Entries",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MessageBytes",
                table: "Entries");

            migrationBuilder.AddColumn<string>(
                name: "MessageJson",
                table: "Entries",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
