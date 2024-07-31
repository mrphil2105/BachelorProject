using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apachi.UserApp.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSubmissionCommitmentSignature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubmissionCommitmentSignature",
                table: "Submissions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "SubmissionCommitmentSignature",
                table: "Submissions",
                type: "BLOB",
                nullable: false,
                defaultValue: new byte[0]);
        }
    }
}
