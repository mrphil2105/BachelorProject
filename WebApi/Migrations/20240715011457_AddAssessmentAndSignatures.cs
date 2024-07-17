using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apachi.WebApi.Migrations
{
    /// <inheritdoc />
    public partial class AddAssessmentAndSignatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Assessment",
                table: "Reviews",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "AssessmentSignature",
                table: "Reviews",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "ReviewCommitmentSignature",
                table: "Reviews",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "ReviewNonceSignature",
                table: "Reviews",
                type: "BLOB",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Assessment",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "AssessmentSignature",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "ReviewCommitmentSignature",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "ReviewNonceSignature",
                table: "Reviews");
        }
    }
}
