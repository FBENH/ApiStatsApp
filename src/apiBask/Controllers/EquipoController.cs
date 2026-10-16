using apiBask.Models;
using apiBask.Models.Request;
using apiBask.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace apiBask.Controllers
{
    [Route("/Equipo")]
    public class EquipoController(BasketContext db) : ApiControllerBase
    {
        private IQueryable<Equipo> MisEquipos => db.Equipos.Where(e => e.IdUsuario == UsuarioActual);

        [HttpPost("Crear")]
        public async Task<IActionResult> CrearEquipo(EquipoRequest model)
        {
            db.Equipos.Add(new Equipo
            {
                IdUsuario = UsuarioActual,
                Nombre = model.Nombre,
                Ciudad = model.Ciudad,
                Entrenador = model.Entrenador,
                Escudo = model.Escudo,
                Activo = true,
            });
            await db.SaveChangesAsync();
            return Exito("Se creó el equipo correctamente.");
        }

        [HttpPost("CrearEquipoRapido")]
        public async Task<IActionResult> CrearEquipoRapido(EquipoRapidoRequest model)
        {
            var equipo = new Equipo
            {
                Nombre = model.Nombre,
                IdUsuario = UsuarioActual,
                Activo = true,
                Jugadors = (model.Jugadores ?? []).Select(j => new Jugador
                {
                    Numero = j.Numero,
                    Nombre = "N/D",
                    IdUsuario = UsuarioActual,
                    Activo = true,
                }).ToList(),
            };
            db.Equipos.Add(equipo);
            await db.SaveChangesAsync();
            return Exito("Se creo el equipo", equipo.Id);
        }

        [HttpDelete("Eliminar")]
        public async Task<IActionResult> EliminarEquipo(int id)
        {
            var equipo = await MisEquipos.Include(e => e.Jugadors).FirstOrDefaultAsync(e => e.Id == id);
            if (equipo != null)
            {
                if (!await EquipoService.TieneDependenciasAsync(db, id))
                {
                    db.Remove(equipo);
                }
                else
                {
                    equipo.Activo = false;
                    foreach (var j in equipo.Jugadors)
                    {
                        j.EquipoId = null;
                        j.Equipo = null;
                    }
                }
                await db.SaveChangesAsync();
            }
            return Exito("Se eliminó el equipo.");
        }

        [HttpPut("Modificar")]
        public async Task<IActionResult> ModificarEquipo(EquipoRequest model)
        {
            var equipo = await MisEquipos.Include(e => e.Jugadors).FirstOrDefaultAsync(e => e.Id == model.Id);
            if (equipo == null) return NoEncontrado("No se encontró el equipo.");

            equipo.Nombre = model.Nombre;
            equipo.Entrenador = model.Entrenador;
            equipo.Ciudad = model.Ciudad;
            if (!string.IsNullOrEmpty(model.Escudo))
            {
                equipo.Escudo = model.Escudo;
            }
            foreach (var j in equipo.Jugadors)
            {
                j.Equipo = model.Nombre;
            }
            await db.SaveChangesAsync();
            return Exito("Se modificó el equipo con éxito.");
        }

        // idUsuario se acepta por compatibilidad con el front, pero se ignora.
        [HttpGet("Listar")]
        public async Task<IActionResult> ListarEquipos(string? idUsuario)
        {
            var lista = await MisEquipos.AsNoTracking().Where(e => e.Activo).ToListAsync();
            return Exito("Se cargó la lista con los equipos", lista);
        }

        [HttpGet("ListarPorId")]
        public async Task<IActionResult> ListarPorId(int idEquipo)
        {
            var equipo = await MisEquipos.AsNoTracking()
                .Include(e => e.EstadisticaPartidos)
                .Include(e => e.Jugadors)
                .FirstOrDefaultAsync(e => e.Id == idEquipo);
            if (equipo == null) return NoEncontrado("No se encontró el equipo.");

            var response = equipo.ToResponse();
            response.Jugadores = equipo.Jugadors.Select(j => j.ToResponse()).ToList();
            response.EstadisticasPartidos = equipo.EstadisticaPartidos.Select(e => e.ToResponse()).ToList();
            return Exito("Se obtuvo el equipo", response);
        }
    }
}
