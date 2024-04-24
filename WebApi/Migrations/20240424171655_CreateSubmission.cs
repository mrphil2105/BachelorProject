using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apachi.WebApi.Migrations
{
    /// <inheritdoc />
    public partial class CreateSubmission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Submissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SubmissionRandomness = table.Column<byte[]>(type: "BLOB", nullable: false),
                    ReviewRandomness = table.Column<byte[]>(type: "BLOB", nullable: false),
                    SubmissionCommitment = table.Column<byte[]>(type: "BLOB", nullable: false),
                    IdentityCommitment = table.Column<byte[]>(type: "BLOB", nullable: false),
                    SubmissionPublicKey = table.Column<byte[]>(type: "BLOB", nullable: false),
                    SubmissionSignature = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Submissions", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Submissions");
        }
    }
}
