using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apachi.UserApp.Migrations
{
    /// <inheritdoc />
    public partial class SubmitterAndReviewer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Submissions_Users_UserId",
                table: "Submissions");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Submissions_UserId",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Submissions");

            migrationBuilder.RenameColumn(
                name: "SecretsHmac",
                table: "Submissions",
                newName: "EncryptedPrivateKey");

            migrationBuilder.RenameColumn(
                name: "EncryptedSecrets",
                table: "Submissions",
                newName: "EncryptedIdentityRandomness");

            migrationBuilder.AddColumn<Guid>(
                name: "SubmitterId",
                table: "Submissions",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "Reviewers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    PasswordSalt = table.Column<byte[]>(type: "BLOB", nullable: false),
                    AuthenticationHash = table.Column<byte[]>(type: "BLOB", nullable: false),
                    EncryptedPrivateKey = table.Column<byte[]>(type: "BLOB", nullable: false),
                    EncryptedSharedKey = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reviewers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Submitters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    PasswordSalt = table.Column<byte[]>(type: "BLOB", nullable: false),
                    AuthenticationHash = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Submitters", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_SubmitterId",
                table: "Submissions",
                column: "SubmitterId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviewers_Username",
                table: "Reviewers",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Submitters_Username",
                table: "Submitters",
                column: "Username",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Submissions_Submitters_SubmitterId",
                table: "Submissions",
                column: "SubmitterId",
                principalTable: "Submitters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Submissions_Submitters_SubmitterId",
                table: "Submissions");

            migrationBuilder.DropTable(
                name: "Reviewers");

            migrationBuilder.DropTable(
                name: "Submitters");

            migrationBuilder.DropIndex(
                name: "IX_Submissions_SubmitterId",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "SubmitterId",
                table: "Submissions");

            migrationBuilder.RenameColumn(
                name: "EncryptedPrivateKey",
                table: "Submissions",
                newName: "SecretsHmac");

            migrationBuilder.RenameColumn(
                name: "EncryptedIdentityRandomness",
                table: "Submissions",
                newName: "EncryptedSecrets");

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Submissions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AuthenticationHash = table.Column<byte[]>(type: "BLOB", nullable: false),
                    PasswordSalt = table.Column<byte[]>(type: "BLOB", nullable: false),
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    Username = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_UserId",
                table: "Submissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Submissions_Users_UserId",
                table: "Submissions",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
