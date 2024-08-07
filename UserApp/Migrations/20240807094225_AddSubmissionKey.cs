using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apachi.UserApp.Migrations
{
    /// <inheritdoc />
    public partial class AddSubmissionKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "EncryptedSubmissionKey",
                table: "Submissions",
                type: "BLOB",
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EncryptedSubmissionKey",
                table: "Submissions");
        }
    }
}
