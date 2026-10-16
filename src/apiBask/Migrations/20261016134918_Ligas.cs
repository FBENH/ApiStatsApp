using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace apiBask.Migrations
{
    /// <inheritdoc />
    public partial class Ligas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Jornada",
                table: "Partidos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TemporadaId",
                table: "Partidos",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Ligas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UsuarioId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ligas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ligas_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Temporadas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LigaId = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Temporadas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Temporadas_Ligas_LigaId",
                        column: x => x.LigaId,
                        principalTable: "Ligas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Divisiones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemporadaId = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Divisiones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Divisiones_Temporadas_TemporadaId",
                        column: x => x.TemporadaId,
                        principalTable: "Temporadas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Inscripciones",
                columns: table => new
                {
                    TemporadaId = table.Column<int>(type: "int", nullable: false),
                    EquipoId = table.Column<int>(type: "int", nullable: false),
                    DivisionId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inscripciones", x => new { x.TemporadaId, x.EquipoId });
                    table.ForeignKey(
                        name: "FK_Inscripciones_Divisiones_DivisionId",
                        column: x => x.DivisionId,
                        principalTable: "Divisiones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Inscripciones_Equipos_EquipoId",
                        column: x => x.EquipoId,
                        principalTable: "Equipos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Inscripciones_Temporadas_TemporadaId",
                        column: x => x.TemporadaId,
                        principalTable: "Temporadas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Partidos_TemporadaId",
                table: "Partidos",
                column: "TemporadaId");

            migrationBuilder.CreateIndex(
                name: "IX_Divisiones_TemporadaId",
                table: "Divisiones",
                column: "TemporadaId");

            migrationBuilder.CreateIndex(
                name: "IX_Inscripciones_DivisionId",
                table: "Inscripciones",
                column: "DivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_Inscripciones_EquipoId",
                table: "Inscripciones",
                column: "EquipoId");

            migrationBuilder.CreateIndex(
                name: "IX_Ligas_Slug",
                table: "Ligas",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ligas_UsuarioId",
                table: "Ligas",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Temporadas_LigaId",
                table: "Temporadas",
                column: "LigaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Partidos_Temporadas_TemporadaId",
                table: "Partidos",
                column: "TemporadaId",
                principalTable: "Temporadas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Partidos_Temporadas_TemporadaId",
                table: "Partidos");

            migrationBuilder.DropTable(
                name: "Inscripciones");

            migrationBuilder.DropTable(
                name: "Divisiones");

            migrationBuilder.DropTable(
                name: "Temporadas");

            migrationBuilder.DropTable(
                name: "Ligas");

            migrationBuilder.DropIndex(
                name: "IX_Partidos_TemporadaId",
                table: "Partidos");

            migrationBuilder.DropColumn(
                name: "Jornada",
                table: "Partidos");

            migrationBuilder.DropColumn(
                name: "TemporadaId",
                table: "Partidos");
        }
    }
}
