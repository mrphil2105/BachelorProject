using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apachi.WebApi.Migrations
{
    /// <inheritdoc />
    public partial class CreateReviewer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Reviewers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReviewerPublicKey = table.Column<byte[]>(type: "BLOB", nullable: false),
                    EncryptedSharedKey = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reviewers", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Reviewers");
        }
    }
}
