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
                name: "Usuario",
                columns: table => new
                {
                    usuario = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    pass = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    email = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuario", x => x.usuario);
                });

            migrationBuilder.CreateTable(
                name: "Equipo",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    ciudad = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    escudo = table.Column<string>(type: "varchar(max)", unicode: false, nullable: true),
                    entrenador = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    id_usuario = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipo", x => x.id);
                    table.ForeignKey(
                        name: "FK_Equipo_Usuario_id_usuario",
                        column: x => x.id_usuario,
                        principalTable: "Usuario",
                        principalColumn: "usuario");
                });

            migrationBuilder.CreateTable(
                name: "Jugador",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    posicion = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    equipo_id = table.Column<int>(type: "int", nullable: true),
                    foto = table.Column<string>(type: "varchar(max)", unicode: false, nullable: true),
                    altura = table.Column<decimal>(type: "decimal(18,0)", nullable: true),
                    peso = table.Column<decimal>(type: "decimal(18,0)", nullable: true),
                    id_usuario = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    activo = table.Column<bool>(type: "bit", nullable: false),
                    equipo = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    numero = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Jugador", x => x.id);
                    table.ForeignKey(
                        name: "FK_Jugador_Equipo_equipo_id",
                        column: x => x.equipo_id,
                        principalTable: "Equipo",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_Jugador_Usuario_id_usuario",
                        column: x => x.id_usuario,
                        principalTable: "Usuario",
                        principalColumn: "usuario");
                });

            migrationBuilder.CreateTable(
                name: "Partido",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    equipo_local = table.Column<int>(type: "int", nullable: true),
                    equipo_visitante = table.Column<int>(type: "int", nullable: true),
                    ganador = table.Column<int>(type: "int", nullable: true),
                    fecha = table.Column<DateTime>(type: "datetime", nullable: true),
                    lugar = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    id_usuario = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Partido", x => x.id);
                    table.ForeignKey(
                        name: "FK_Partido_Equipo_equipo_local",
                        column: x => x.equipo_local,
                        principalTable: "Equipo",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_Partido_Equipo_equipo_visitante",
                        column: x => x.equipo_visitante,
                        principalTable: "Equipo",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_Partido_Usuario_id_usuario",
                        column: x => x.id_usuario,
                        principalTable: "Usuario",
                        principalColumn: "usuario");
                });

            migrationBuilder.CreateTable(
                name: "EstadisticaJugador",
                columns: table => new
                {
                    jugador_id = table.Column<int>(type: "int", nullable: false),
                    partido_id = table.Column<int>(type: "int", nullable: false),
                    equipo_id = table.Column<int>(type: "int", nullable: true),
                    puntos = table.Column<int>(type: "int", nullable: true),
                    asistencias = table.Column<int>(type: "int", nullable: true),
                    rebotes = table.Column<int>(type: "int", nullable: true),
                    rebotes_ofensivos = table.Column<int>(type: "int", nullable: true),
                    rebotes_defensivos = table.Column<int>(type: "int", nullable: true),
                    robos = table.Column<int>(type: "int", nullable: true),
                    tapones = table.Column<int>(type: "int", nullable: true),
                    intentos_2_puntos = table.Column<int>(type: "int", nullable: true),
                    aciertos_2_puntos = table.Column<int>(type: "int", nullable: true),
                    intentos_3_puntos = table.Column<int>(type: "int", nullable: true),
                    aciertos_3_puntos = table.Column<int>(type: "int", nullable: true),
                    intentos_1_punto = table.Column<int>(type: "int", nullable: true),
                    aciertos_1_punto = table.Column<int>(type: "int", nullable: true),
                    faltas = table.Column<int>(type: "int", nullable: true),
                    minutos = table.Column<TimeSpan>(type: "time", nullable: true),
                    perdidas = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstadisticaJugador", x => new { x.jugador_id, x.partido_id });
                    table.ForeignKey(
                        name: "FK_EstadisticaJugador_Equipo_equipo_id",
                        column: x => x.equipo_id,
                        principalTable: "Equipo",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_EstadisticaJugador_Jugador_jugador_id",
                        column: x => x.jugador_id,
                        principalTable: "Jugador",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_EstadisticaJugador_Partido_partido_id",
                        column: x => x.partido_id,
                        principalTable: "Partido",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "EstadisticaPartido",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    partido_id = table.Column<int>(type: "int", nullable: false),
                    equipo_id = table.Column<int>(type: "int", nullable: false),
                    puntos = table.Column<int>(type: "int", nullable: true),
                    asistencias = table.Column<int>(type: "int", nullable: true),
                    rebotes = table.Column<int>(type: "int", nullable: true),
                    rebotes_ofensivos = table.Column<int>(type: "int", nullable: true),
                    rebotes_defensivos = table.Column<int>(type: "int", nullable: true),
                    robos = table.Column<int>(type: "int", nullable: true),
                    tapones = table.Column<int>(type: "int", nullable: true),
                    intentos_2_puntos = table.Column<int>(type: "int", nullable: true),
                    aciertos_2_puntos = table.Column<int>(type: "int", nullable: true),
                    intentos_3_puntos = table.Column<int>(type: "int", nullable: true),
                    aciertos_3_puntos = table.Column<int>(type: "int", nullable: true),
                    intentos_1_punto = table.Column<int>(type: "int", nullable: true),
                    aciertos_1_punto = table.Column<int>(type: "int", nullable: true),
                    perdidas = table.Column<int>(type: "int", nullable: true),
                    faltas = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstadisticaPartido", x => x.id);
                    table.ForeignKey(
                        name: "FK_EstadisticaPartido_Equipo_equipo_id",
                        column: x => x.equipo_id,
                        principalTable: "Equipo",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_EstadisticaPartido_Partido_partido_id",
                        column: x => x.partido_id,
                        principalTable: "Partido",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Equipo_id_usuario",
                table: "Equipo",
                column: "id_usuario");

            migrationBuilder.CreateIndex(
                name: "IX_EstadisticaJugador_equipo_id",
                table: "EstadisticaJugador",
                column: "equipo_id");

            migrationBuilder.CreateIndex(
                name: "IX_EstadisticaJugador_partido_id",
                table: "EstadisticaJugador",
                column: "partido_id");

            migrationBuilder.CreateIndex(
                name: "IX_EstadisticaPartido_equipo_id",
                table: "EstadisticaPartido",
                column: "equipo_id");

            migrationBuilder.CreateIndex(
                name: "IX_EstadisticaPartido_partido_id",
                table: "EstadisticaPartido",
                column: "partido_id");

            migrationBuilder.CreateIndex(
                name: "IX_Jugador_equipo_id",
                table: "Jugador",
                column: "equipo_id");

            migrationBuilder.CreateIndex(
                name: "IX_Jugador_id_usuario",
                table: "Jugador",
                column: "id_usuario");

            migrationBuilder.CreateIndex(
                name: "IX_Partido_equipo_local",
                table: "Partido",
                column: "equipo_local");

            migrationBuilder.CreateIndex(
                name: "IX_Partido_equipo_visitante",
                table: "Partido",
                column: "equipo_visitante");

            migrationBuilder.CreateIndex(
                name: "IX_Partido_id_usuario",
                table: "Partido",
                column: "id_usuario");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EstadisticaJugador");

            migrationBuilder.DropTable(
                name: "EstadisticaPartido");

            migrationBuilder.DropTable(
                name: "Jugador");

            migrationBuilder.DropTable(
                name: "Partido");

            migrationBuilder.DropTable(
                name: "Equipo");

            migrationBuilder.DropTable(
                name: "Usuario");
        }
    }
}
