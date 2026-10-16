using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace apiBask.Migrations
{
    /// <inheritdoc />
    public partial class CreadaEnLiga : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreadaEn",
                table: "Ligas",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreadaEn",
                table: "Ligas");
        }
    }
}
