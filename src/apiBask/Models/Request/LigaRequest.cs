namespace apiBask.Models.Request
{
    public record LigaRequest(int? Id, string Nombre, string? Slug, string? Descripcion, string? Temporada);

    public record TemporadaRequest(int LigaId, string Nombre);

    public record DivisionRequest(int TemporadaId, string Nombre);

    public record InscripcionDto(int EquipoId, int? DivisionId);

    // Reemplaza la lista de equipos de la temporada.
    public record InscripcionesRequest(int TemporadaId, List<InscripcionDto> Equipos);

    public record PartidoFixtureDto(int LocalId, int VisitanteId, DateTime? Fecha, string? Lugar, int? Jornada);

    public record FixtureRequest(int TemporadaId, List<PartidoFixtureDto> Partidos);

    // Arma el fixture de todos contra todos de cada división, una fecha cada `DiasEntreFechas` días.
    public record GenerarFixtureRequest(int TemporadaId, DateTime? Desde, int DiasEntreFechas = 7, bool IdaYVuelta = false);
}
