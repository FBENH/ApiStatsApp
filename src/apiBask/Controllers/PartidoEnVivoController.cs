using apiBask.Models;
using apiBask.Models.Request;
using apiBask.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace apiBask.Controllers
{
    // Partidos que se anotan jugada a jugada. Cada jugada se guarda apenas se carga,
    // así el partido se puede retomar desde otro dispositivo si se corta la conexión.
    [Route("/Partido")]
    public class PartidoEnVivoController(BasketContext db, RegistroDePartido registro, AvisosEnVivo avisos) : ApiControllerBase
    {
        private IQueryable<Partido> MisPartidos =>
            db.Partidos.Where(p => p.UsuarioId == UsuarioActual && p.Activo);

        [HttpPost("Iniciar")]
        public async Task<IActionResult> Iniciar(IniciarPartidoRequest model)
        {
            if (model.PartidoId is int programadoId) return await IniciarProgramadoAsync(programadoId, model);
            if (model.EquipoLocal == model.EquipoVisitante)
                return Invalido("El local y el visitante tienen que ser equipos distintos.");
            if (model.DuracionCuartoMin is < 1 or > 20)
                return Invalido("Los cuartos tienen que durar entre 1 y 20 minutos.");
            var propios = await db.Equipos.CountAsync(e =>
                (e.Id == model.EquipoLocal || e.Id == model.EquipoVisitante) && e.UsuarioId == UsuarioActual);
            if (propios != 2) return Invalido("Los equipos del partido tienen que ser tuyos.");

            var partido = new Partido
            {
                UsuarioId = UsuarioActual,
                LocalId = model.EquipoLocal,
                VisitanteId = model.EquipoVisitante,
                Fecha = model.Fecha,
                Lugar = model.Lugar,
                DuracionCuartoMin = model.DuracionCuartoMin,
                Estado = EstadoPartido.EnCurso,
                Activo = true,
            };
            db.Partidos.Add(partido);
            await db.SaveChangesAsync();
            return Exito("Se inició el partido.", partido.Id);
        }

        private async Task<IActionResult> IniciarProgramadoAsync(int id, IniciarPartidoRequest model)
        {
            if (model.DuracionCuartoMin is < 1 or > 20)
                return Invalido("Los cuartos tienen que durar entre 1 y 20 minutos.");
            var partido = await MisPartidos.FirstOrDefaultAsync(p => p.Id == id);
            if (partido == null) return NoEncontrado("No se encontró el partido.");
            if (partido.Estado != EstadoPartido.Programado) return Invalido("Ese partido ya se empezó a anotar.");

            partido.Estado = EstadoPartido.EnCurso;
            partido.DuracionCuartoMin = model.DuracionCuartoMin;
            partido.Fecha = model.Fecha ?? partido.Fecha;
            partido.Lugar = model.Lugar ?? partido.Lugar;
            await db.SaveChangesAsync();
            await avisos.PartidoAsync(partido);
            return Exito("Se inició el partido.", partido.Id);
        }

        [HttpGet("EnCurso")]
        public async Task<IActionResult> EnCurso()
        {
            var partidos = await MisPartidos.AsNoTracking()
                .Where(p => p.Estado == EstadoPartido.EnCurso)
                .OrderByDescending(p => p.Id)
                .Select(p => new
                {
                    id = p.Id,
                    local = p.Local.Nombre,
                    visitante = p.Visitante.Nombre,
                    equipoLocalId = p.LocalId,
                    equipoVisitanteId = p.VisitanteId,
                    fecha = p.Fecha,
                    lugar = p.Lugar,
                    duracionCuartoMin = p.DuracionCuartoMin,
                    jugadas = p.Jugadas.Count,
                })
                .ToListAsync();
            return Exito("Se obtuvieron los partidos en curso.", partidos);
        }

        [HttpGet("Jugadas")]
        public async Task<IActionResult> Jugadas(int partidoId)
        {
            if (!await MisPartidos.AnyAsync(p => p.Id == partidoId)) return NoEncontrado("No se encontró el partido.");
            var jugadas = await db.Jugadas.AsNoTracking()
                .Where(j => j.PartidoId == partidoId)
                .OrderBy(j => j.Numero)
                .Select(j => new JugadaDto(j.Id, j.Numero, j.Cuarto, j.RelojMs, j.Tipo, j.EquipoId, j.JugadorId,
                    j.SaleId, j.Acierto, j.Valor, j.Ofensivo, j.X, j.Y, j.RelacionadaId))
                .ToListAsync();
            return Exito("Se obtuvieron las jugadas.", jugadas);
        }

        // Guarda un lote de jugadas. Las que ya estaban (mismo id) se ignoran, así un reintento no duplica nada.
        [HttpPost("Jugadas")]
        public async Task<IActionResult> AgregarJugadas(int partidoId, List<JugadaDto> jugadas)
        {
            var partido = await MisPartidos.FirstOrDefaultAsync(p => p.Id == partidoId);
            if (partido == null) return NoEncontrado("No se encontró el partido.");
            var error = await registro.ValidarAsync(partido, jugadas);
            if (error != null) return Invalido(error);
            return Exito("Se guardaron las jugadas.", await registro.AgregarAsync(partido, jugadas));
        }

        // Deshacer: borra una jugada. Si ya no existe, no es un error (puede ser un reintento).
        [HttpDelete("Jugadas")]
        public async Task<IActionResult> BorrarJugada(int partidoId, Guid id)
        {
            var partido = await MisPartidos.FirstOrDefaultAsync(p => p.Id == partidoId);
            if (partido == null) return NoEncontrado("No se encontró el partido.");
            await registro.BorrarAsync(partido, id);
            return Exito("Se borró la jugada.");
        }

        // Calcula la planilla a partir de las jugadas y marca el partido como terminado.
        // Se puede llamar de nuevo después de corregir jugadas.
        [HttpPost("Finalizar")]
        public async Task<IActionResult> Finalizar(int id, FinalizarRequest? fin)
        {
            var partido = await MisPartidos.FirstOrDefaultAsync(p => p.Id == id);
            if (partido == null) return NoEncontrado("No se encontró el partido.");
            if (!await db.Jugadas.AnyAsync(j => j.PartidoId == id))
                return Invalido("El partido no tiene jugadas.");
            if (fin is { RelojMs: < 0 } or { Cuarto: < 1 })
                return Invalido("El cuarto o el reloj final no son válidos.");

            await registro.FinalizarAsync(partido, fin is { Cuarto: int c } ? (c, fin.RelojMs ?? 0) : null);
            return Exito("Se terminó el partido.", partido.Id);
        }
    }
}
