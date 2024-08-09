using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apachi.UserApp.Migrations
{
    /// <inheritdoc />
    public partial class RenameToUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LogEvents_Reviewers_ReviewerId",
                table: "LogEvents");

            migrationBuilder.DropIndex(
                name: "IX_LogEvents_ReviewerId",
                table: "LogEvents");

            migrationBuilder.RenameColumn(
                name: "ReviewerId",
                table: "LogEvents",
                newName: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "LogEvents",
                newName: "ReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_LogEvents_ReviewerId",
                table: "LogEvents",
                column: "ReviewerId");

            migrationBuilder.AddForeignKey(
                name: "FK_LogEvents_Reviewers_ReviewerId",
                table: "LogEvents",
                column: "ReviewerId",
                principalTable: "Reviewers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
