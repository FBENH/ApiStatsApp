using apiBask.Models;
using apiBask.Models.Request;
using apiBask.Models.Response;
using apiBask.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace apiBask.Controllers
{
    // API de solo lectura para la página pública de una liga, que puede estar en otro dominio.
    // No pide login: muestra los partidos de las temporadas de la liga y nada más del usuario.
    [ApiController]
    [AllowAnonymous]
    [Route("/publico/ligas/{slug}")]
    public class PublicoController(BasketContext db) : ControllerBase
    {
        private const int CantidadDeLideres = 10;

        [HttpGet]
        public async Task<IActionResult> Liga(string slug, int? temporada)
        {
            var (liga, actual) = await BuscarAsync(slug, temporada);
            if (actual == null) return NoEncontrado();
            var divisiones = await db.Divisiones.AsNoTracking().Where(d => d.TemporadaId == actual.Id)
                .OrderBy(d => d.Id).Select(d => new { d.Id, d.Nombre }).ToListAsync();
            var equipos = await db.Inscripciones.AsNoTracking().Where(i => i.TemporadaId == actual.Id)
                .OrderBy(i => i.Equipo.Nombre)
                .Select(i => new { id = i.EquipoId, i.Equipo.Nombre, i.Equipo.Escudo, i.DivisionId })
                .ToListAsync();
            return Exito(new
            {
                liga!.Nombre,
                liga.Slug,
                liga.Descripcion,
                temporadas = liga.Temporadas.Select(t => new { t.Id, t.Nombre }),
                temporada = new { actual.Id, actual.Nombre, divisiones, equipos },
            });
        }

        // Fixture, resultados y partidos en vivo, con el MVP de cada partido terminado.
        [HttpGet("partidos")]
        public async Task<IActionResult> Partidos(string slug, int? temporada)
        {
            var (_, actual) = await BuscarAsync(slug, temporada);
            if (actual == null) return NoEncontrado();
            var partidos = await PartidosDeAsync(db, actual.Id);
            var estadisticas = await EstadisticasAsync(actual.Id);
            var jugadores = await JugadoresAsync(estadisticas.Select(e => e.JugadorId));
            var mvps = estadisticas.GroupBy(e => e.PartidoId)
                .ToDictionary(g => g.Key, g => g.MaxBy(TablaLiga.Valoracion)!);
            return Exito(partidos.Select(p => new
            {
                p.Id, p.Jornada, p.Fecha, p.Lugar, p.Estado, p.Local, p.Visitante,
                mvp = mvps.TryGetValue(p.Id, out var e)
                    ? new { jugador = jugadores[e.JugadorId], equipoId = e.EquipoId, valoracion = TablaLiga.Valoracion(e), puntos = e.Puntos }
                    : null,
            }));
        }

        // Un partido con su planilla y su registro de jugadas. Mientras se juega, la planilla sale de las jugadas.
        [HttpGet("partidos/{id:int}")]
        public async Task<IActionResult> Partido(string slug, int id)
        {
            var partido = await db.Partidos.AsNoTracking()
                .Include(p => p.Local).Include(p => p.Visitante).Include(p => p.Temporada)
                .FirstOrDefaultAsync(p => p.Id == id && p.Activo && p.Temporada!.Liga.Slug == slug);
            if (partido == null) return NoEncontrado();

            var jugadas = await db.Jugadas.AsNoTracking().Where(j => j.PartidoId == id).OrderBy(j => j.Numero).ToListAsync();
            List<EstadisticaJugador> planilla;
            List<EstadisticaPartido> equipos;
            if (jugadas.Count > 0)
            {
                (planilla, equipos) = Planilla.Calcular(partido, jugadas);
            }
            else
            {
                planilla = await db.EstadisticasJugador.AsNoTracking().Where(e => e.PartidoId == id).ToListAsync();
                equipos = await db.EstadisticasPartido.AsNoTracking().Where(e => e.PartidoId == id).ToListAsync();
            }
            var conEstadisticas = planilla.Select(e => e.JugadorId).ToList();
            var jugadores = await db.Jugadores.AsNoTracking()
                .Where(j => (j.EquipoId == partido.LocalId || j.EquipoId == partido.VisitanteId) && j.Activo
                    || conEstadisticas.Contains(j.Id))
                .OrderBy(j => j.Numero)
                .Select(j => new { j.Id, j.Nombre, j.Numero, j.EquipoId, j.Foto })
                .ToListAsync();

            return Exito(new
            {
                partido.Id,
                partido.Jornada,
                partido.Fecha,
                partido.Lugar,
                partido.Estado,
                partido.DuracionCuartoMin,
                temporada = new { partido.Temporada!.Id, partido.Temporada.Nombre },
                local = new { id = partido.LocalId, partido.Local.Nombre, partido.Local.Escudo },
                visitante = new { id = partido.VisitanteId, partido.Visitante.Nombre, partido.Visitante.Escudo },
                jugadores,
                planilla = planilla.Select(e => new { estadistica = e.ToResponse(), valoracion = TablaLiga.Valoracion(e) }),
                equipos = equipos.Select(e => e.ToResponse()),
                jugadas = jugadas.Select(j => new JugadaDto(j.Id, j.Numero, j.Cuarto, j.RelojMs, j.Tipo, j.EquipoId, j.JugadorId,
                    j.SaleId, j.Acierto, j.Valor, j.Ofensivo, j.X, j.Y, j.RelacionadaId)),
            });
        }

        [HttpGet("posiciones")]
        public async Task<IActionResult> Posiciones(string slug, int? temporada)
        {
            var (_, actual) = await BuscarAsync(slug, temporada);
            if (actual == null) return NoEncontrado();
            var inscriptos = await db.Inscripciones.AsNoTracking().Where(i => i.TemporadaId == actual.Id)
                .Select(i => new { i.EquipoId, i.Equipo.Nombre, i.Equipo.Escudo, i.DivisionId, division = i.Division!.Nombre })
                .ToListAsync();
            var resultados = (await PartidosDeAsync(db, actual.Id))
                .Where(p => p.Estado == EstadoPartido.Finalizado)
                .Select(p => new TablaLiga.Resultado(p.Id, p.Fecha, p.Local.Id, p.Visitante.Id, p.Local.Puntos ?? 0, p.Visitante.Puntos ?? 0))
                .ToList();
            var equipos = inscriptos.ToDictionary(i => i.EquipoId);

            var tablas = inscriptos
                .GroupBy(i => (i.DivisionId, i.division))
                .OrderBy(g => g.Key.DivisionId)
                .Select(g => new
                {
                    division = g.Key.DivisionId == null ? null : new { id = g.Key.DivisionId, nombre = g.Key.division },
                    filas = TablaLiga.Posiciones(g.Select(i => i.EquipoId), resultados).Select(f => new
                    {
                        equipo = new { id = f.EquipoId, equipos[f.EquipoId].Nombre, equipos[f.EquipoId].Escudo },
                        jugados = f.Jugados,
                        ganados = f.Ganados,
                        perdidos = f.Perdidos,
                        porcentaje = f.Porcentaje,
                        puntosAFavor = f.PuntosAFavor,
                        puntosEnContra = f.PuntosEnContra,
                        diferencia = f.Diferencia,
                        puntos = f.PuntosTabla,
                    }),
                });
            return Exito(tablas);
        }

        // Promedios por partido. Por defecto entran los que jugaron al menos la mitad de los partidos del que más jugó.
        [HttpGet("lideres")]
        public async Task<IActionResult> Lideres(string slug, int? temporada, int? minPartidos)
        {
            var (_, actual) = await BuscarAsync(slug, temporada);
            if (actual == null) return NoEncontrado();
            var estadisticas = await EstadisticasAsync(actual.Id);
            var minimo = Math.Max(1, minPartidos ?? TablaLiga.MinimoDePartidos(estadisticas));
            var categorias = TablaLiga.Categorias
                .Select(c => (c, lideres: TablaLiga.Lideres(estadisticas, c.Valor, minimo, CantidadDeLideres)))
                .ToList();
            var jugadores = await JugadoresAsync(categorias.SelectMany(c => c.lideres.Select(l => l.JugadorId)));
            return Exito(new
            {
                minPartidos = minimo,
                categorias = categorias.Select(x => new
                {
                    clave = x.c.Clave,
                    nombre = x.c.Nombre,
                    jugadores = x.lideres.Select(l => new { jugador = jugadores[l.JugadorId], l.Partidos, l.Promedio, l.Total }),
                }),
            });
        }

        // MVP de la semana y del mes (más valoración sumada) y de la temporada (mejor promedio con el mínimo de partidos).
        // La semana y el mes son los de la fecha pedida o, si no se pide, los del último partido jugado.
        [HttpGet("mvp")]
        public async Task<IActionResult> Mvp(string slug, int? temporada, DateTime? fecha)
        {
            var (_, actual) = await BuscarAsync(slug, temporada);
            if (actual == null) return NoEncontrado();
            var estadisticas = await EstadisticasAsync(actual.Id);
            var fechas = await db.Partidos.AsNoTracking()
                .Where(p => p.TemporadaId == actual.Id && p.Estado == EstadoPartido.Finalizado && p.Activo && p.Fecha != null)
                .ToDictionaryAsync(p => p.Id, p => p.Fecha!.Value);
            var referencia = fecha ?? (fechas.Count > 0 ? fechas.Values.Max() : DateTime.UtcNow);

            TablaLiga.Lider? EnPeriodo((DateTime Desde, DateTime Hasta) periodo) => TablaLiga.MasValioso(
                estadisticas.Where(e => fechas.TryGetValue(e.PartidoId, out var f) && f >= periodo.Desde && f < periodo.Hasta));
            var semana = TablaLiga.Semana(referencia);
            var mes = TablaLiga.Mes(referencia);
            var mvps = new
            {
                semana = EnPeriodo(semana),
                mes = EnPeriodo(mes),
                temporada = TablaLiga.Lideres(estadisticas, TablaLiga.Valoracion, TablaLiga.MinimoDePartidos(estadisticas), 1)
                    .FirstOrDefault(),
            };
            var jugadores = await JugadoresAsync(new[] { mvps.semana, mvps.mes, mvps.temporada }.OfType<TablaLiga.Lider>().Select(l => l.JugadorId));
            object? Datos(TablaLiga.Lider? l) => l == null ? null
                : new { jugador = jugadores[l.JugadorId], l.Partidos, valoracion = l.Total, promedio = l.Promedio };

            return Exito(new
            {
                semana = new { desde = semana.Desde, hasta = semana.Hasta.AddDays(-1), mvp = Datos(mvps.semana) },
                mes = new { desde = mes.Desde, hasta = mes.Hasta.AddDays(-1), mvp = Datos(mvps.mes) },
                temporada = new { mvp = Datos(mvps.temporada) },
            });
        }

        // Ficha de un jugador: promedios en cada temporada de la liga y sus partidos.
        [HttpGet("jugadores/{id:int}")]
        public async Task<IActionResult> Jugador(string slug, int id)
        {
            var estadisticas = await db.EstadisticasJugador.AsNoTracking()
                .Where(e => e.JugadorId == id && e.Partido.Activo && e.Partido.Estado == EstadoPartido.Finalizado
                    && e.Partido.Temporada!.Liga.Slug == slug)
                .Select(e => new
                {
                    e,
                    temporadaId = e.Partido.TemporadaId!.Value,
                    temporada = e.Partido.Temporada!.Nombre,
                    e.Partido.Fecha,
                    rival = e.EquipoId == e.Partido.LocalId ? e.Partido.Visitante.Nombre : e.Partido.Local.Nombre,
                })
                .ToListAsync();
            if (estadisticas.Count == 0) return NoEncontrado();
            var jugador = (await JugadoresAsync([id]))[id];

            return Exito(new
            {
                jugador,
                temporadas = estadisticas.GroupBy(x => (x.temporadaId, x.temporada)).OrderBy(g => g.Key.temporadaId).Select(g => new
                {
                    id = g.Key.temporadaId,
                    nombre = g.Key.temporada,
                    partidos = g.Count(),
                    promedios = TablaLiga.Categorias.ToDictionary(c => c.Clave, c => Math.Round(g.Average(x => c.Valor(x.e)), 1)),
                }),
                partidos = estadisticas.OrderByDescending(x => x.Fecha).Select(x => new
                {
                    partidoId = x.e.PartidoId,
                    x.Fecha,
                    x.rival,
                    estadistica = x.e.ToResponse(),
                    valoracion = TablaLiga.Valoracion(x.e),
                }),
            });
        }

        public record EquipoEnPartido(int Id, string Nombre, string? Escudo, int? Puntos);
        public record PartidoDeLiga(int Id, int? Jornada, DateTime? Fecha, string? Lugar, EstadoPartido Estado,
            EquipoEnPartido Local, EquipoEnPartido Visitante);

        // Los partidos de una temporada con el marcador: el guardado si terminó, o la suma de las jugadas si se está jugando.
        public static async Task<List<PartidoDeLiga>> PartidosDeAsync(BasketContext db, int temporadaId)
        {
            var filas = await db.Partidos.AsNoTracking()
                .Where(p => p.TemporadaId == temporadaId && p.Activo)
                .OrderBy(p => p.Jornada).ThenBy(p => p.Fecha).ThenBy(p => p.Id)
                .Select(p => new
                {
                    p.Id, p.Jornada, p.Fecha, p.Lugar, p.Estado,
                    p.LocalId, local = p.Local.Nombre, localEscudo = p.Local.Escudo,
                    p.VisitanteId, visitante = p.Visitante.Nombre, visitanteEscudo = p.Visitante.Escudo,
                    guardados = p.EstadisticasPartido.Select(e => new { e.EquipoId, e.Puntos }).ToList(),
                    enVivo = p.Jugadas
                        .Where(j => j.Acierto == true && (j.Tipo == TipoJugada.Tiro || j.Tipo == TipoJugada.Libre))
                        .GroupBy(j => j.EquipoId)
                        .Select(g => new { EquipoId = g.Key, Puntos = g.Sum(j => j.Tipo == TipoJugada.Libre ? 1 : j.Valor ?? 0) })
                        .ToList(),
                })
                .AsSplitQuery()
                .ToListAsync();

            return filas.Select(p =>
            {
                int? Puntos(int equipoId) => p.Estado switch
                {
                    EstadoPartido.Programado => null,
                    EstadoPartido.EnCurso => p.enVivo.FirstOrDefault(e => e.EquipoId == equipoId)?.Puntos ?? 0,
                    _ => p.guardados.FirstOrDefault(e => e.EquipoId == equipoId)?.Puntos ?? 0,
                };
                return new PartidoDeLiga(p.Id, p.Jornada, p.Fecha, p.Lugar, p.Estado,
                    new(p.LocalId, p.local, p.localEscudo, Puntos(p.LocalId)),
                    new(p.VisitanteId, p.visitante, p.visitanteEscudo, Puntos(p.VisitanteId)));
            }).ToList();
        }

        private async Task<(Liga? Liga, Temporada? Temporada)> BuscarAsync(string slug, int? temporada)
        {
            var liga = await db.Ligas.AsNoTracking().Include(l => l.Temporadas.OrderBy(t => t.Id))
                .FirstOrDefaultAsync(l => l.Slug == slug);
            var actual = temporada == null ? liga?.Temporadas.LastOrDefault() : liga?.Temporadas.FirstOrDefault(t => t.Id == temporada);
            return (liga, actual);
        }

        // Planillas de los partidos terminados de la temporada.
        private Task<List<EstadisticaJugador>> EstadisticasAsync(int temporadaId) =>
            db.EstadisticasJugador.AsNoTracking()
                .Where(e => e.Partido.TemporadaId == temporadaId && e.Partido.Estado == EstadoPartido.Finalizado && e.Partido.Activo)
                .ToListAsync();

        public record JugadorPublico(int Id, string Nombre, int Numero, string? Foto, int? EquipoId, string? Equipo);

        private async Task<Dictionary<int, JugadorPublico>> JugadoresAsync(IEnumerable<int> ids)
        {
            var lista = ids.Distinct().ToList();
            return await db.Jugadores.AsNoTracking().Where(j => lista.Contains(j.Id))
                .Select(j => new JugadorPublico(j.Id, j.Nombre, j.Numero, j.Foto, j.EquipoId, j.Equipo!.Nombre))
                .ToDictionaryAsync(j => j.Id);
        }

        private OkObjectResult Exito(object data) => Ok(new Response { exito = 1, mensaje = "Ok", data = data });

        private NotFoundObjectResult NoEncontrado() => NotFound(new Response { mensaje = "No se encontró." });
    }
}
