using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apachi.WebApi.Migrations
{
    /// <inheritdoc />
    public partial class ShareAssessments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "AssessmentsSetSignature",
                table: "Submissions",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "EncryptedAssessmentsSet",
                table: "Submissions",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "GradeRandomness",
                table: "Submissions",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "GroupKey",
                table: "Submissions",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "EncryptedGradeRandomness",
                table: "Reviews",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "EncryptedGroupKey",
                table: "Reviews",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "GradeRandomnessSignature",
                table: "Reviews",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "GroupKeySignature",
                table: "Reviews",
                type: "BLOB",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssessmentsSetSignature",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "EncryptedAssessmentsSet",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "GradeRandomness",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "GroupKey",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "EncryptedGradeRandomness",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "EncryptedGroupKey",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "GradeRandomnessSignature",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "GroupKeySignature",
                table: "Reviews");
        }
    }
}
