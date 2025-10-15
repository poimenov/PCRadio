using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PCRadio.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddedHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HistoryRecords",
                columns: table => new
                {
                    StartTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TrackName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    StationName = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoryRecords", x => x.StartTime);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HistoryRecords");
        }
    }
}
