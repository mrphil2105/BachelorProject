using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apachi.UserApp.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewerId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ReviewerId",
                table: "LogEvents",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LogEvents_Reviewers_ReviewerId",
                table: "LogEvents");

            migrationBuilder.DropIndex(
                name: "IX_LogEvents_ReviewerId",
                table: "LogEvents");

            migrationBuilder.DropColumn(
                name: "ReviewerId",
                table: "LogEvents");
        }
    }
}
