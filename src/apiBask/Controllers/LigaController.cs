using System.Text;
using System.Text.RegularExpressions;
using apiBask.Models;
using apiBask.Models.Request;
using apiBask.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace apiBask.Controllers
{
    // Gestión de las ligas del usuario: temporadas, divisiones, equipos inscriptos y fixture.
    // Lo que ve el público está en PublicoController.
    [Route("/Liga")]
    public partial class LigaController(BasketContext db) : ApiControllerBase
    {
        private IQueryable<Liga> MisLigas => db.Ligas.Where(l => l.UsuarioId == UsuarioActual);
        private IQueryable<Temporada> MisTemporadas => db.Temporadas.Where(t => t.Liga.UsuarioId == UsuarioActual);

        [HttpGet("Listar")]
        public async Task<IActionResult> Listar()
        {
            var ligas = await MisLigas.AsNoTracking()
                .OrderBy(l => l.Nombre)
                .Select(l => new { l.Id, l.Nombre, l.Slug, l.Descripcion, temporadas = l.Temporadas.Count })
                .ToListAsync();
            return Exito("Se obtuvieron las ligas.", ligas);
        }

        [HttpPost("Crear")]
        public async Task<IActionResult> Crear(LigaRequest model)
        {
            var (slug, error) = await ValidarAsync(model, null);
            if (error != null) return Invalido(error);
            var liga = new Liga
            {
                Nombre = model.Nombre.Trim(),
                Slug = slug!,
                Descripcion = Vacio(model.Descripcion),
                UsuarioId = UsuarioActual,
            };
            // Toda liga empieza con una temporada para poder inscribir equipos enseguida.
            liga.Temporadas.Add(new Temporada { Nombre = Vacio(model.Temporada) ?? DateTime.UtcNow.Year.ToString() });
            db.Ligas.Add(liga);
            await db.SaveChangesAsync();
            return Exito("Se creó la liga.", liga.Id);
        }

        [HttpPut("Modificar")]
        public async Task<IActionResult> Modificar(LigaRequest model)
        {
            var liga = await MisLigas.FirstOrDefaultAsync(l => l.Id == model.Id);
            if (liga == null) return NoEncontrado("No se encontró la liga.");
            var (slug, error) = await ValidarAsync(model, liga.Id);
            if (error != null) return Invalido(error);
            liga.Nombre = model.Nombre.Trim();
            liga.Slug = slug!;
            liga.Descripcion = Vacio(model.Descripcion);
            await db.SaveChangesAsync();
            return Exito("Se modificó la liga.");
        }

        // Solo se puede borrar una liga sin partidos empezados; los programados se borran con ella.
        [HttpDelete("Eliminar")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var liga = await MisLigas.FirstOrDefaultAsync(l => l.Id == id);
            if (liga == null) return NoEncontrado("No se encontró la liga.");
            var partidos = db.Partidos.Where(p => p.Temporada!.LigaId == id);
            if (await partidos.AnyAsync(p => p.Estado != EstadoPartido.Programado))
                return Invalido("La liga tiene partidos jugados y no se puede borrar.");

            await using var transaccion = await db.Database.BeginTransactionAsync();
            await partidos.ExecuteDeleteAsync();
            await db.Inscripciones.Where(i => i.Temporada.LigaId == id).ExecuteDeleteAsync();
            await db.Divisiones.Where(d => d.Temporada.LigaId == id).ExecuteDeleteAsync();
            await db.Temporadas.Where(t => t.LigaId == id).ExecuteDeleteAsync();
            await db.Ligas.Where(l => l.Id == id).ExecuteDeleteAsync();
            await transaccion.CommitAsync();
            return Exito("Se eliminó la liga.");
        }

        // Todo lo que hace falta para administrar una temporada (por defecto, la última).
        [HttpGet("Obtener")]
        public async Task<IActionResult> Obtener(int id, int? temporada)
        {
            var liga = await MisLigas.AsNoTracking()
                .Include(l => l.Temporadas.OrderBy(t => t.Id))
                .FirstOrDefaultAsync(l => l.Id == id);
            if (liga == null) return NoEncontrado("No se encontró la liga.");
            var actual = temporada == null ? liga.Temporadas.LastOrDefault() : liga.Temporadas.FirstOrDefault(t => t.Id == temporada);
            if (actual == null) return NoEncontrado("No se encontró la temporada.");

            var divisiones = await db.Divisiones.AsNoTracking().Where(d => d.TemporadaId == actual.Id)
                .OrderBy(d => d.Id).Select(d => new { d.Id, d.Nombre }).ToListAsync();
            var equipos = await db.Inscripciones.AsNoTracking().Where(i => i.TemporadaId == actual.Id)
                .OrderBy(i => i.Equipo.Nombre)
                .Select(i => new { id = i.EquipoId, i.Equipo.Nombre, i.Equipo.Escudo, i.DivisionId })
                .ToListAsync();
            var partidos = await PublicoController.PartidosDeAsync(db, actual.Id);

            return Exito("Se obtuvo la liga.", new
            {
                liga.Id,
                liga.Nombre,
                liga.Slug,
                liga.Descripcion,
                temporadas = liga.Temporadas.Select(t => new { t.Id, t.Nombre }),
                temporada = new { actual.Id, actual.Nombre, divisiones, equipos, partidos },
            });
        }

        [HttpPost("Temporada")]
        public async Task<IActionResult> CrearTemporada(TemporadaRequest model)
        {
            if (!await MisLigas.AnyAsync(l => l.Id == model.LigaId)) return NoEncontrado("No se encontró la liga.");
            var nombre = Vacio(model.Nombre);
            if (nombre == null || nombre.Length > 40) return Invalido("El nombre de la temporada tiene que tener entre 1 y 40 caracteres.");
            var temporada = new Temporada { LigaId = model.LigaId, Nombre = nombre };
            db.Temporadas.Add(temporada);
            await db.SaveChangesAsync();
            return Exito("Se creó la temporada.", temporada.Id);
        }

        [HttpPost("Division")]
        public async Task<IActionResult> CrearDivision(DivisionRequest model)
        {
            if (!await MisTemporadas.AnyAsync(t => t.Id == model.TemporadaId)) return NoEncontrado("No se encontró la temporada.");
            var nombre = Vacio(model.Nombre);
            if (nombre == null || nombre.Length > 40) return Invalido("El nombre de la división tiene que tener entre 1 y 40 caracteres.");
            var division = new Division { TemporadaId = model.TemporadaId, Nombre = nombre };
            db.Divisiones.Add(division);
            await db.SaveChangesAsync();
            return Exito("Se creó la división.", division.Id);
        }

        // Los equipos de la división quedan sin división.
        [HttpDelete("Division")]
        public async Task<IActionResult> EliminarDivision(int id)
        {
            var division = await db.Divisiones.FirstOrDefaultAsync(d => d.Id == id && d.Temporada.Liga.UsuarioId == UsuarioActual);
            if (division == null) return NoEncontrado("No se encontró la división.");
            await using var transaccion = await db.Database.BeginTransactionAsync();
            await db.Inscripciones.Where(i => i.DivisionId == id).ExecuteUpdateAsync(s => s.SetProperty(i => i.DivisionId, (int?)null));
            db.Divisiones.Remove(division);
            await db.SaveChangesAsync();
            await transaccion.CommitAsync();
            return Exito("Se eliminó la división.");
        }

        [HttpPut("Inscripciones")]
        public async Task<IActionResult> Inscripciones(InscripcionesRequest model)
        {
            var temporada = await MisTemporadas.Include(t => t.Inscripciones).Include(t => t.Divisiones)
                .FirstOrDefaultAsync(t => t.Id == model.TemporadaId);
            if (temporada == null) return NoEncontrado("No se encontró la temporada.");
            if (model.Equipos.Select(e => e.EquipoId).Distinct().Count() != model.Equipos.Count)
                return Invalido("Hay equipos repetidos.");
            if (model.Equipos.Any(e => e.DivisionId != null && temporada.Divisiones.All(d => d.Id != e.DivisionId)))
                return Invalido("La división no es de esta temporada.");
            var ids = model.Equipos.Select(e => e.EquipoId).ToList();
            if (await db.Equipos.CountAsync(e => ids.Contains(e.Id) && e.UsuarioId == UsuarioActual && e.Activo) != ids.Count)
                return Invalido("Los equipos tienen que ser tuyos.");

            var quitados = temporada.Inscripciones.Where(i => !ids.Contains(i.EquipoId)).Select(i => i.EquipoId).ToList();
            if (await db.Partidos.AnyAsync(p => p.TemporadaId == temporada.Id && p.Activo
                    && (quitados.Contains(p.LocalId) || quitados.Contains(p.VisitanteId))))
                return Invalido("No se puede sacar un equipo que ya tiene partidos en la temporada.");

            temporada.Inscripciones.RemoveAll(i => quitados.Contains(i.EquipoId));
            foreach (var e in model.Equipos)
            {
                var inscripcion = temporada.Inscripciones.FirstOrDefault(i => i.EquipoId == e.EquipoId);
                if (inscripcion == null) temporada.Inscripciones.Add(new Inscripcion { EquipoId = e.EquipoId, DivisionId = e.DivisionId });
                else inscripcion.DivisionId = e.DivisionId;
            }
            await db.SaveChangesAsync();
            return Exito("Se guardaron los equipos de la temporada.");
        }

        [HttpPost("Partidos")]
        public async Task<IActionResult> AgregarPartidos(FixtureRequest model)
        {
            var temporada = await MisTemporadas.AsNoTracking().Include(t => t.Inscripciones)
                .FirstOrDefaultAsync(t => t.Id == model.TemporadaId);
            if (temporada == null) return NoEncontrado("No se encontró la temporada.");
            if (model.Partidos.Count is 0 or > 500) return Invalido("Se pueden agregar de 1 a 500 partidos por vez.");
            var inscriptos = temporada.Inscripciones.Select(i => i.EquipoId).ToHashSet();
            foreach (var p in model.Partidos)
            {
                if (p.LocalId == p.VisitanteId) return Invalido("El local y el visitante tienen que ser equipos distintos.");
                if (!inscriptos.Contains(p.LocalId) || !inscriptos.Contains(p.VisitanteId))
                    return Invalido("Los dos equipos tienen que estar inscriptos en la temporada.");
                if (p.Lugar?.Length > 100) return Invalido("El lugar puede tener hasta 100 caracteres.");
            }
            var partidos = model.Partidos.Select(p => Programado(temporada.Id, p.LocalId, p.VisitanteId, p.Fecha, p.Jornada, p.Lugar)).ToList();
            db.Partidos.AddRange(partidos);
            await db.SaveChangesAsync();
            return Exito("Se agregaron los partidos.", partidos.Select(p => p.Id));
        }

        [HttpPost("GenerarFixture")]
        public async Task<IActionResult> GenerarFixture(GenerarFixtureRequest model)
        {
            var temporada = await MisTemporadas.AsNoTracking().Include(t => t.Inscripciones)
                .FirstOrDefaultAsync(t => t.Id == model.TemporadaId);
            if (temporada == null) return NoEncontrado("No se encontró la temporada.");
            if (model.DiasEntreFechas is < 1 or > 60) return Invalido("Entre fechas tiene que haber de 1 a 60 días.");
            if (await db.Partidos.AnyAsync(p => p.TemporadaId == temporada.Id && p.Activo))
                return Invalido("La temporada ya tiene partidos. Borralos para generar el fixture de nuevo.");

            // Cada división juega su propio todos contra todos; las fechas coinciden entre divisiones.
            var grupos = temporada.Inscripciones.GroupBy(i => i.DivisionId).Select(g => g.Select(i => i.EquipoId).Order().ToList());
            var cruces = grupos.SelectMany(g => TablaLiga.TodosContraTodos(g, model.IdaYVuelta)).ToList();
            if (cruces.Count == 0) return Invalido("Hacen falta al menos dos equipos en la misma división.");
            var partidos = cruces
                .OrderBy(c => c.Jornada)
                .Select(c => Programado(temporada.Id, c.LocalId, c.VisitanteId,
                    model.Desde?.Date.AddDays((c.Jornada - 1) * model.DiasEntreFechas), c.Jornada, null))
                .ToList();
            db.Partidos.AddRange(partidos);
            await db.SaveChangesAsync();
            return Exito($"Se generaron {partidos.Count} partidos.", partidos.Count);
        }

        private Partido Programado(int temporadaId, int localId, int visitanteId, DateTime? fecha, int? jornada, string? lugar) => new()
        {
            UsuarioId = UsuarioActual,
            TemporadaId = temporadaId,
            LocalId = localId,
            VisitanteId = visitanteId,
            Fecha = fecha,
            Jornada = jornada,
            Lugar = Vacio(lugar),
            Estado = EstadoPartido.Programado,
            Activo = true,
        };

        private async Task<(string? Slug, string? Error)> ValidarAsync(LigaRequest model, int? id)
        {
            var nombre = Vacio(model.Nombre);
            if (nombre == null || nombre.Length > 60) return (null, "El nombre tiene que tener entre 1 y 60 caracteres.");
            if (model.Descripcion?.Length > 500) return (null, "La descripción puede tener hasta 500 caracteres.");
            var slug = ASlug(Vacio(model.Slug) ?? nombre);
            if (slug.Length is < 3 or > 60) return (null, "La dirección tiene que tener entre 3 y 60 letras, números o guiones.");
            if (await db.Ligas.AnyAsync(l => l.Slug == slug && l.Id != id))
                return (null, $"Ya hay una liga con la dirección \"{slug}\". Elegí otra.");
            return (slug, null);
        }

        // "Liga del Barrio 2026" -> "liga-del-barrio-2026"
        public static string ASlug(string texto)
        {
            var sinTildes = new string(texto.Normalize(NormalizationForm.FormD)
                .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                .ToArray());
            return NoAlfanumerico().Replace(sinTildes.ToLowerInvariant(), "-").Trim('-');
        }

        private static string? Vacio(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

        [GeneratedRegex("[^a-z0-9]+")]
        private static partial Regex NoAlfanumerico();
    }
}
