using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apachi.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Reviewers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PublicKey = table.Column<byte[]>(type: "bytea", nullable: false),
                    EncryptedSharedKey = table.Column<byte[]>(type: "bytea", nullable: false),
                    SharedKeySignature = table.Column<byte[]>(type: "bytea", nullable: false)
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
