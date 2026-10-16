using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace apiBask.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Pass = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Equipos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Ciudad = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Escudo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Entrenador = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Equipos_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Jugadores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Posicion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    EquipoId = table.Column<int>(type: "int", nullable: true),
                    Foto = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Altura = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: true),
                    Peso = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: true),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Jugadores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Jugadores_Equipos_EquipoId",
                        column: x => x.EquipoId,
                        principalTable: "Equipos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Jugadores_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Partidos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LocalId = table.Column<int>(type: "int", nullable: false),
                    VisitanteId = table.Column<int>(type: "int", nullable: false),
                    GanadorId = table.Column<int>(type: "int", nullable: true),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Lugar = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Partidos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Partidos_Equipos_GanadorId",
                        column: x => x.GanadorId,
                        principalTable: "Equipos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Partidos_Equipos_LocalId",
                        column: x => x.LocalId,
                        principalTable: "Equipos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Partidos_Equipos_VisitanteId",
                        column: x => x.VisitanteId,
                        principalTable: "Equipos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Partidos_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EstadisticasJugador",
                columns: table => new
                {
                    PartidoId = table.Column<int>(type: "int", nullable: false),
                    JugadorId = table.Column<int>(type: "int", nullable: false),
                    Minutos = table.Column<TimeSpan>(type: "time", nullable: false),
                    EquipoId = table.Column<int>(type: "int", nullable: false),
                    Puntos = table.Column<int>(type: "int", nullable: false),
                    Asistencias = table.Column<int>(type: "int", nullable: false),
                    Rebotes = table.Column<int>(type: "int", nullable: false),
                    RebotesOfensivos = table.Column<int>(type: "int", nullable: false),
                    RebotesDefensivos = table.Column<int>(type: "int", nullable: false),
                    Robos = table.Column<int>(type: "int", nullable: false),
                    Tapones = table.Column<int>(type: "int", nullable: false),
                    Intentos1Punto = table.Column<int>(type: "int", nullable: false),
                    Aciertos1Punto = table.Column<int>(type: "int", nullable: false),
                    Intentos2Puntos = table.Column<int>(type: "int", nullable: false),
                    Aciertos2Puntos = table.Column<int>(type: "int", nullable: false),
                    Intentos3Puntos = table.Column<int>(type: "int", nullable: false),
                    Aciertos3Puntos = table.Column<int>(type: "int", nullable: false),
                    Faltas = table.Column<int>(type: "int", nullable: false),
                    Perdidas = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstadisticasJugador", x => new { x.PartidoId, x.JugadorId });
                    table.ForeignKey(
                        name: "FK_EstadisticasJugador_Equipos_EquipoId",
                        column: x => x.EquipoId,
                        principalTable: "Equipos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EstadisticasJugador_Jugadores_JugadorId",
                        column: x => x.JugadorId,
                        principalTable: "Jugadores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EstadisticasJugador_Partidos_PartidoId",
                        column: x => x.PartidoId,
                        principalTable: "Partidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EstadisticasPartido",
                columns: table => new
                {
                    PartidoId = table.Column<int>(type: "int", nullable: false),
                    EquipoId = table.Column<int>(type: "int", nullable: false),
                    Puntos = table.Column<int>(type: "int", nullable: false),
                    Asistencias = table.Column<int>(type: "int", nullable: false),
                    Rebotes = table.Column<int>(type: "int", nullable: false),
                    RebotesOfensivos = table.Column<int>(type: "int", nullable: false),
                    RebotesDefensivos = table.Column<int>(type: "int", nullable: false),
                    Robos = table.Column<int>(type: "int", nullable: false),
                    Tapones = table.Column<int>(type: "int", nullable: false),
                    Intentos1Punto = table.Column<int>(type: "int", nullable: false),
                    Aciertos1Punto = table.Column<int>(type: "int", nullable: false),
                    Intentos2Puntos = table.Column<int>(type: "int", nullable: false),
                    Aciertos2Puntos = table.Column<int>(type: "int", nullable: false),
                    Intentos3Puntos = table.Column<int>(type: "int", nullable: false),
                    Aciertos3Puntos = table.Column<int>(type: "int", nullable: false),
                    Faltas = table.Column<int>(type: "int", nullable: false),
                    Perdidas = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstadisticasPartido", x => new { x.PartidoId, x.EquipoId });
                    table.ForeignKey(
                        name: "FK_EstadisticasPartido_Equipos_EquipoId",
                        column: x => x.EquipoId,
                        principalTable: "Equipos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EstadisticasPartido_Partidos_PartidoId",
                        column: x => x.PartidoId,
                        principalTable: "Partidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Equipos_UsuarioId",
                table: "Equipos",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_EstadisticasJugador_EquipoId",
                table: "EstadisticasJugador",
                column: "EquipoId");

            migrationBuilder.CreateIndex(
                name: "IX_EstadisticasJugador_JugadorId",
                table: "EstadisticasJugador",
                column: "JugadorId");

            migrationBuilder.CreateIndex(
                name: "IX_EstadisticasPartido_EquipoId",
                table: "EstadisticasPartido",
                column: "EquipoId");

            migrationBuilder.CreateIndex(
                name: "IX_Jugadores_EquipoId",
                table: "Jugadores",
                column: "EquipoId");

            migrationBuilder.CreateIndex(
                name: "IX_Jugadores_UsuarioId",
                table: "Jugadores",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Partidos_GanadorId",
                table: "Partidos",
                column: "GanadorId");

            migrationBuilder.CreateIndex(
                name: "IX_Partidos_LocalId",
                table: "Partidos",
                column: "LocalId");

            migrationBuilder.CreateIndex(
                name: "IX_Partidos_UsuarioId",
                table: "Partidos",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Partidos_VisitanteId",
                table: "Partidos",
                column: "VisitanteId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Nombre",
                table: "Usuarios",
                column: "Nombre",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EstadisticasJugador");

            migrationBuilder.DropTable(
                name: "EstadisticasPartido");

            migrationBuilder.DropTable(
                name: "Jugadores");

            migrationBuilder.DropTable(
                name: "Partidos");

            migrationBuilder.DropTable(
                name: "Equipos");

            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
