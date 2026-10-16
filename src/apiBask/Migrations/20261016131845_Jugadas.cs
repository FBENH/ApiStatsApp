using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace apiBask.Migrations
{
    /// <inheritdoc />
    public partial class Jugadas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DuracionCuartoMin",
                table: "Partidos",
                type: "int",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.AddColumn<int>(
                name: "Estado",
                table: "Partidos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Jugadas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PartidoId = table.Column<int>(type: "int", nullable: false),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    Cuarto = table.Column<int>(type: "int", nullable: false),
                    RelojMs = table.Column<int>(type: "int", nullable: true),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EquipoId = table.Column<int>(type: "int", nullable: false),
                    JugadorId = table.Column<int>(type: "int", nullable: true),
                    SaleId = table.Column<int>(type: "int", nullable: true),
                    Acierto = table.Column<bool>(type: "bit", nullable: true),
                    Valor = table.Column<int>(type: "int", nullable: true),
                    Ofensivo = table.Column<bool>(type: "bit", nullable: true),
                    X = table.Column<double>(type: "float", nullable: true),
                    Y = table.Column<double>(type: "float", nullable: true),
                    RelacionadaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreadaEn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Jugadas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Jugadas_Equipos_EquipoId",
                        column: x => x.EquipoId,
                        principalTable: "Equipos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Jugadas_Jugadores_JugadorId",
                        column: x => x.JugadorId,
                        principalTable: "Jugadores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Jugadas_Jugadores_SaleId",
                        column: x => x.SaleId,
                        principalTable: "Jugadores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Jugadas_Partidos_PartidoId",
                        column: x => x.PartidoId,
                        principalTable: "Partidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Jugadas_EquipoId",
                table: "Jugadas",
                column: "EquipoId");

            migrationBuilder.CreateIndex(
                name: "IX_Jugadas_JugadorId",
                table: "Jugadas",
                column: "JugadorId");

            migrationBuilder.CreateIndex(
                name: "IX_Jugadas_PartidoId_Numero",
                table: "Jugadas",
                columns: new[] { "PartidoId", "Numero" });

            migrationBuilder.CreateIndex(
                name: "IX_Jugadas_SaleId",
                table: "Jugadas",
                column: "SaleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Jugadas");

            migrationBuilder.DropColumn(
                name: "DuracionCuartoMin",
                table: "Partidos");

            migrationBuilder.DropColumn(
                name: "Estado",
                table: "Partidos");
        }
    }
}
