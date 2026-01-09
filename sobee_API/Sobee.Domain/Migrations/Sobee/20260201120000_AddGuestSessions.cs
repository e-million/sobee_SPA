using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sobee.Domain.Migrations.Sobee
{
    /// <inheritdoc />
    public partial class AddGuestSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GuestSessions",
                columns: table => new
                {
                    session_id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    secret = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    expires_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_seen_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("GuestSessions_PK", x => x.session_id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuestSessions");
        }
    }
}
